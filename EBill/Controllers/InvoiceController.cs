using System.Security.Claims;
using EBill.Data;
using EBill.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EBill.Controllers
{
    [Authorize]
    public class InvoiceController : Controller
    {
        private readonly ApplicationDbContext _db;

        public InvoiceController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Customer());
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? days)
        {
            var query = _db.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Products)
                .Include(i => i.Taxes)
                .AsQueryable();

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
        public async Task<IActionResult> Delete(int id, int? days)
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

            return RedirectToAction(nameof(Index), new { days });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _db.Customers.Add(model);
            await _db.SaveChangesAsync();

            var invoice = new Invoice
            {
                CustomerId = model.Id,
                Date = DateTime.UtcNow,
            };

            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index", "Product", new { invoiceId = invoice.Id });
        }

        // Bills out a quotation: creates a new invoice for the same customer and
        // links the quotation's existing line items to it (rather than duplicating
        // them), so the same Product rows are shared between the quotation and the
        // invoice. The quotation record itself is left untouched.
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
    }
}
