using System.Security.Claims;
using EBill.Data;
using EBill.Models;
using EBill.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EBill.Controllers
{
    [Authorize]
    public class InvoiceController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IDispatchService _dispatchService;

        public InvoiceController(ApplicationDbContext db, IDispatchService dispatchService)
        {
            _db = db;
            _dispatchService = dispatchService;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Customer());
        }

        // Reached from the "Create Invoice" button on the Customer List (only shown
        // once this company is connected to Stock Management - see
        // CustomersController.Index). Unlike the blank-form Create() above, the
        // customer is already known; this page instead shows what Stock Management
        // dispatched to them on the selected date (see IDispatchService/
        // DispatchService) so an invoice can be built straight from that.
        [HttpGet]
        public async Task<IActionResult> CreateFromCustomer(int customerId, DateTime? date)
        {
            var companyIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(companyIdClaim, out var companyId))
            {
                return Unauthorized();
            }

            var customer = await _db.Customers
                .FirstOrDefaultAsync(c => c.Id == customerId && c.CompanyId == companyId);

            if (customer is null)
            {
                return NotFound();
            }

            // Dispatched items can only be pulled for a day that's already fully
            // logged in Stock Management - today's (and any future) dispatches
            // aren't final yet, so the filter is capped at yesterday. Defaults to
            // yesterday when no date is given, and silently clamps down to it if
            // someone requests today/a future date directly (e.g. by editing the
            // URL - the date input itself also enforces this via its max attribute).
            var maxAllowedDate = DateTime.Today;
            var requestedDate = (date ?? maxAllowedDate).Date;
            var selectedDate = requestedDate > maxAllowedDate ? maxAllowedDate : requestedDate;

            var items = await _dispatchService.GetDispatchedItemsAsync(companyId, customerId, selectedDate);

            return View(new CreateInvoiceFromDispatchViewModel
            {
                Customer = customer,
                SelectedDate = selectedDate,
                MaxSelectableDate = maxAllowedDate,
                DateWasAdjusted = requestedDate > maxAllowedDate,
                Items = items,
            });
        }

        // Reached via the "Invoice Items" link on CreateFromCustomer, next to
        // "Dispatched Items" - shows just the items for this customer/date that have
        // ALREADY been invoiced, which CreateFromCustomer's own table no longer lists
        // (it now only shows pending ones, so the form there always maps directly to
        // "items still to bill"). Reuses the same view model and IDispatchService
        // lookup as CreateFromCustomer since the underlying data (and date paging) is
        // identical - just filtered the other way.
        [HttpGet]
        public async Task<IActionResult> InvoicedItems(int customerId, DateTime? date)
        {
            var companyIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(companyIdClaim, out var companyId))
            {
                return Unauthorized();
            }

            var customer = await _db.Customers
                .FirstOrDefaultAsync(c => c.Id == customerId && c.CompanyId == companyId);

            if (customer is null)
            {
                return NotFound();
            }

            var maxAllowedDate = DateTime.Today;
            var requestedDate = (date ?? maxAllowedDate).Date;
            var selectedDate = requestedDate > maxAllowedDate ? maxAllowedDate : requestedDate;

            var items = await _dispatchService.GetDispatchedItemsAsync(companyId, customerId, selectedDate);

            return View(new CreateInvoiceFromDispatchViewModel
            {
                Customer = customer,
                SelectedDate = selectedDate,
                MaxSelectableDate = maxAllowedDate,
                DateWasAdjusted = requestedDate > maxAllowedDate,
                Items = items.Where(i => i.IsInvoiced).ToList(),
            });
        }

        // Saves the invoice + one Product line item per row shown on
        // CreateFromCustomer's page, then hands off to the existing Product/Index
        // page (same place the blank-form Create() flow lands on) so tax
        // calculation, viewing, etc. all keep working unchanged.
        //
        // Takes the line items from the posted form (items[0].Rate, items[1].Rate,
        // ...) rather than re-querying IDispatchService, because the "Cost/Qty"
        // input on that page lets the user edit each item's rate before creating
        // the invoice - re-fetching would silently discard those edits. Same trust
        // level as ProductController.Add's manual line-item entry: both accept
        // whatever an authenticated company posts for its own invoice.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFromDispatch(int customerId, DateTime date, List<DispatchLineItemInput> items)
        {
            var companyIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(companyIdClaim, out var companyId))
            {
                return Unauthorized();
            }

            var customer = await _db.Customers
                .FirstOrDefaultAsync(c => c.Id == customerId && c.CompanyId == companyId);

            if (customer is null)
            {
                return NotFound();
            }

            var selectedDate = date.Date;

            if (items is null || !items.Any())
            {
                TempData["DispatchError"] = "No dispatched items found for that date - nothing to invoice.";
                return RedirectToAction(nameof(CreateFromCustomer), new { customerId, date = selectedDate });
            }

            // DispatchProductId is unique per Product row (ApplicationDbContext), so
            // billing the same dispatched item twice would otherwise fail with a raw
            // DB exception. Checked up front so a re-submitted or already-invoiced
            // page gives a plain error instead.
            var dispatchIds = items
                .Select(i => i.DispatchProductId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToList();

            if (dispatchIds.Count > 0)
            {
                var alreadyInvoiced = await _db.Products
                    .Where(p => p.DispatchProductId != null && dispatchIds.Contains(p.DispatchProductId))
                    .Select(p => p.DispatchProductId!)
                    .ToListAsync();

                if (alreadyInvoiced.Any())
                {
                    TempData["DispatchError"] = "One or more of these items have already been invoiced: " + string.Join(", ", alreadyInvoiced.Distinct()) + ".";
                    return RedirectToAction(nameof(CreateFromCustomer), new { customerId, date = selectedDate });
                }
            }

            var invoice = new Invoice
            {
                CustomerId = customer.Id,
                Date = DateTime.UtcNow,
            };

            await AssignNextBillNumberAsync(invoice, companyId);

            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();

            foreach (var item in items)
            {
                _db.Products.Add(new Product
                {
                    CustomerId = customer.Id,
                    InvoiceId = invoice.Id,
                    DispatchProductId = string.IsNullOrWhiteSpace(item.DispatchProductId) ? null : item.DispatchProductId,
                    Description = item.Description,
                    HsnSac = item.HsnSac,
                    Qty = item.Qty.ToString("0.##"),
                    QtyType = item.QtyType,
                    Rate = item.Rate,
                    Discount = 0,
                    TaxRate = GstRate.Zero,
                    TotalAmount = item.Qty * item.Rate,
                });
            }

            await _db.SaveChangesAsync();

            return RedirectToAction("Index", "Product", new { invoiceId = invoice.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? days, int? customerId)
        {
            int? Companyid = GetCompanyId();
            var query = _db.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Products)
                .Include(i => i.Taxes).Where(i => i.Customer.CompanyId == Companyid)
                .AsQueryable();

            string? customerName = null;
            if (customerId.HasValue)
            {
                query = query.Where(i => i.CustomerId == customerId.Value);
                customerName = await _db.Customers
                    .Where(c => c.Id == customerId.Value && c.CompanyId == Companyid)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync();
            }

            if (days.HasValue && days.Value > 0)
            {
                var fromDate = DateTime.UtcNow.Date.AddDays(-(days.Value - 1));
                query = query.Where(i => i.Date >= fromDate);
            }

            var invoices = await query
                .OrderByDescending(i => i.Date)
                .ThenByDescending(i => i.Id)
                .ToListAsync();

            ViewData["SelectedDays"] = days;
            ViewData["CustomerId"] = customerId;
            ViewData["CustomerName"] = customerName;
            return View(invoices);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accounting(int? days)
        {
            // The report is a fixed snapshot of "now" at creation time, so the link
            // keeps showing the same period even after new invoices are added later.
            var toDate = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);
            var fromDate = (days.HasValue && days.Value > 0)
                ? DateTime.UtcNow.Date.AddDays(-(days.Value - 1))
                : DateTime.MinValue;

            var link = new AccountingLink
            {
                Id = Guid.NewGuid(),
                Days = days,
                FromDate = fromDate,
                ToDate = toDate,
                CreatedDate = DateTime.UtcNow,
            };

            _db.AccountingLinks.Add(link);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index", "Accounting", new { id = link.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int? days, int? customerId)
        {
            var invoice = await _db.Invoices
                .Include(i => i.Products)
                .Include(i => i.Taxes)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice is null)
            {
                return NotFound();
            }

            // Products and Taxes are configured with DeleteBehavior.Restrict, so they
            // must be removed explicitly before the invoice itself can be deleted.
            // A product that's also linked to a quotation (see CreateFromQuotation)
            // is only unlinked from this invoice, not deleted, since the quotation
            // still needs it.
            var sharedWithQuotation = invoice.Products.Where(p => p.QuotationId.HasValue).ToList();
            var invoiceOnlyProducts = invoice.Products.Where(p => !p.QuotationId.HasValue).ToList();

            foreach (var product in sharedWithQuotation)
            {
                product.InvoiceId = null;
            }

            _db.Products.RemoveRange(invoiceOnlyProducts);
            _db.Taxes.RemoveRange(invoice.Taxes);
            _db.Invoices.Remove(invoice);

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { days, customerId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer model)
        {
            var companyIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(companyIdClaim, out var companyId))
            {
                return Unauthorized();
            }

            model.CompanyId = companyId;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Phone number is unique per company, so a customer with this number may
            // already exist here - most likely because the Create page's phone-number
            // lookup (CustomerController.LookupByPhone) just auto-filled these very
            // details from them. Reuse that record (refreshing it with whatever's on
            // the form now) instead of trying to insert a second row, which would trip
            // the unique (CompanyId, PhoneNumber) index.
            var existingCustomer = await _db.Customers
                .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.PhoneNumber == model.PhoneNumber);

            Customer customer;
            if (existingCustomer is not null)
            {
                existingCustomer.Name = model.Name;
                existingCustomer.Address = model.Address;
                existingCustomer.Gstin = model.Gstin;
                customer = existingCustomer;
            }
            else
            {
                _db.Customers.Add(model);
                customer = model;
            }

            await _db.SaveChangesAsync();

            var invoice = new Invoice
            {
                CustomerId = customer.Id,
                Date = DateTime.UtcNow,
            };

            await AssignNextBillNumberAsync(invoice, companyId);

            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index", "Product", new { invoiceId = invoice.Id });
        }
  
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFromQuotation(int quotationId)
        {
            var quotation = await _db.Quotations
                .Include(q => q.Products)
                .FirstOrDefaultAsync(q => q.Id == quotationId);

            if (quotation is null)
            {
                return NotFound();
            }

            var invoice = new Invoice
            {
                CustomerId = quotation.CustomerId,
                QuotationId = quotation.Id,
                Date = DateTime.UtcNow,
                TotalAmount = quotation.Products.Sum(p => p.TotalAmount),
            };

            var companyIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(companyIdClaim, out var companyId))
            {
                await AssignNextBillNumberAsync(invoice, companyId);
            }

            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();

            // Link the quotation's line items to the new invoice - QuotationId stays
            // set, so each Product row now belongs to both.
            foreach (var item in quotation.Products)
            {
                item.InvoiceId = invoice.Id;
            }

            await _db.SaveChangesAsync();

            return RedirectToAction("Index", "Product", new { invoiceId = invoice.Id });
        }

        // Pulls the next auto-numbering value from Settings (see
        // SettingsController.UpdateNumbering) and stamps it onto the invoice, then
        // advances the counter for next time. No-op (BillNumber stays blank, exactly
        // like before this feature existed) if the company hasn't set a starting
        // number yet.
        private async Task AssignNextBillNumberAsync(Invoice invoice, int companyId)
        {
            var settings = await _db.Settings.FirstOrDefaultAsync(s => s.CompanyId == companyId);
            if (settings?.NextInvoiceNumber is int nextNumber)
            {
                invoice.BillNumber = nextNumber.ToString();
                settings.NextInvoiceNumber = nextNumber + 1;
            }
        }

        [HttpGet]
        public async Task<IActionResult> View(int id)
        {
            var invoice = await _db.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Products)
                .Include(i => i.Taxes)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice is null)
            {
                return NotFound();
            }

            Company? company = null;
            var companyIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(companyIdClaim, out var companyId))
            {
                company = await _db.Companies.FindAsync(companyId);
            }

            ViewData["Company"] = company;
            ViewData["InvoiceFootprint"] = company is null
                ? null
                : (await _db.Settings.AsNoTracking()
                    .Where(st => st.CompanyId == company.Id)
                    .Select(st => st.InvoiceFootprint)
                    .FirstOrDefaultAsync());

            // "Next"/"Previous" step through invoices in creation order (by Id),
            // which lines up with BillNumber since it's generated the same way.
            ViewData["PreviousInvoiceId"] = await _db.Invoices
                .Where(i => i.Id < id)
                .OrderByDescending(i => i.Id)
                .Select(i => (int?)i.Id)
                .FirstOrDefaultAsync();

            ViewData["NextInvoiceId"] = await _db.Invoices
                .Where(i => i.Id > id)
                .OrderBy(i => i.Id)
                .Select(i => (int?)i.Id)
                .FirstOrDefaultAsync();

            return View(invoice);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePayment(int id, decimal amountPaid)
        {
            if (amountPaid < 0)
            {
                return BadRequest(new { success = false, message = "Amount paid cannot be negative." });
            }

            var invoice = await _db.Invoices.FindAsync(id);

            if (invoice is null)
            {
                return NotFound();
            }

            invoice.AmountPaid = amountPaid;
            invoice.PaymentStatus = PaymentStatus.Paid;

            await _db.SaveChangesAsync();

            return Ok(new { success = true, amountPaid = invoice.AmountPaid, paymentStatus = invoice.PaymentStatus.ToString() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkEmailed(int id)
        {
            var invoice = await _db.Invoices.FindAsync(id);
            if (invoice is null)
            {
                return NotFound();
            }

            invoice.Emailed = true;
            await _db.SaveChangesAsync();

            return Ok(new { success = true });
        }

        private int? GetCompanyId()
        {
            var companyIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(companyIdClaim, out var companyId) ? companyId : null;
        }
    }
}
