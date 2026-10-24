using System.Globalization;
using EBill.Data;
using EBill.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EBill.Controllers
{
    [Authorize(Roles = UserRoles.Admin)]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ProductController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int invoiceId)
        {
            var invoice = await _db.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Products)
                .Include(i => i.Taxes)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice is null)
            {
                return NotFound();
            }

            return View(invoice);
        }

        [HttpGet]
        public async Task<IActionResult> QuotationIndex(int quotationId)
        {
            var quotation = await _db.Quotations
                .Include(q => q.Customer)
                .Include(q => q.Products)
                .FirstOrDefaultAsync(q => q.Id == quotationId);

            if (quotation is null)
            {
                return NotFound();
            }

            return View(quotation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int invoiceId, [FromForm] ProductLineItemInput input)
        {
            var qtyFieldValid = !ModelState.TryGetValue(nameof(input.Qty), out var qtyState) || qtyState.Errors.Count == 0;

            if (qtyFieldValid &&
                decimal.Parse(input.Qty, NumberStyles.Number, CultureInfo.InvariantCulture) <= 0)
            {
                ModelState.AddModelError(nameof(input.Qty), "Quantity must be greater than 0.");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(kvp => kvp.Value?.Errors.Count > 0)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

                return BadRequest(new { success = false, errors });
            }

            var qtyValue = decimal.Parse(input.Qty, NumberStyles.Number, CultureInfo.InvariantCulture);

            var invoice = await _db.Invoices.FindAsync(invoiceId);
            if (invoice is null)
            {
                return NotFound();
            }

            var product = new Product
            {
                CustomerId = invoice.CustomerId,
                InvoiceId = invoice.Id,
                Description = input.Description,
                HsnSac = input.HsnSac,
                Qty = input.Qty,
                QtyType = input.QtyType,
                Rate = input.Rate,
                Discount = input.Discount,
                TaxRate = input.TaxRate,
                TotalAmount = (qtyValue * input.Rate) - input.Discount,
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            var subtotal = await _db.Products
                .Where(p => p.InvoiceId == invoiceId)
                .SumAsync(p => p.TotalAmount);

            return Ok(new
            {
                success = true,
                product = new
                {
                    product.Id,
                    product.Description,
                    product.HsnSac,
                    product.Qty,
                    product.QtyType,
                    product.Rate,
                    product.Discount,
                    TaxRate = (int)product.TaxRate,
                    product.TotalAmount,
                },
                subtotal
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToQuotation(int quotationId, [FromForm] QuotationLineItemInput input)
        {
            var qtyFieldValid = !ModelState.TryGetValue(nameof(input.Qty), out var qtyState) || qtyState.Errors.Count == 0;

            if (qtyFieldValid &&
                decimal.Parse(input.Qty, NumberStyles.Number, CultureInfo.InvariantCulture) <= 0)
            {
                ModelState.AddModelError(nameof(input.Qty), "Quantity must be greater than 0.");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(kvp => kvp.Value?.Errors.Count > 0)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

                return BadRequest(new { success = false, errors });
            }

            var qtyValue = decimal.Parse(input.Qty, NumberStyles.Number, CultureInfo.InvariantCulture);

            var quotation = await _db.Quotations.FindAsync(quotationId);
            if (quotation is null)
            {
                return NotFound();
            }

            var product = new Product
            {
                CustomerId = quotation.CustomerId,
                QuotationId = quotation.Id,
                Description = input.Description,
                HsnSac = input.HsnSac,
                Qty = input.Qty,
                QtyType = input.QtyType,
                Rate = input.Rate,
                Discount = input.Discount,
                TaxRate = GstRate.Zero,
                TotalAmount = (qtyValue * input.Rate) - input.Discount,
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            var subtotal = await _db.Products
                .Where(p => p.QuotationId == quotationId)
                .SumAsync(p => p.TotalAmount);

            // Quotations have no tax step, so the stored total is always just the line-item subtotal.
            quotation.TotalAmount = subtotal;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                product = new
                {
                    product.Id,
                    product.Description,
                    product.HsnSac,
                    product.Qty,
                    product.QtyType,
                    product.Rate,
                    product.Discount,
                    product.TotalAmount,
                },
                subtotal
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id, int? invoiceId, int? quotationId)
        {
            var product = await _db.Products.FindAsync(id);
            if (product is null)
            {
                return NotFound();
            }

            // A product can be linked to both an invoice and the quotation it was
            // billed from (see InvoiceController.CreateFromQuotation), so removing it
            // from one page should only unlink it from that side. It's only actually
            // deleted once neither side references it any more.
            if (invoiceId.HasValue && product.InvoiceId == invoiceId)
            {
                product.InvoiceId = null;
            }

            if (quotationId.HasValue && product.QuotationId == quotationId)
            {
                product.QuotationId = null;
            }

            if (product.InvoiceId is null && product.QuotationId is null)
            {
                _db.Products.Remove(product);
            }

            await _db.SaveChangesAsync();

            decimal subtotal;
            if (quotationId.HasValue)
            {
                subtotal = await _db.Products
                    .Where(p => p.QuotationId == quotationId)
                    .SumAsync(p => p.TotalAmount);

                var quotation = await _db.Quotations.FindAsync(quotationId.Value);
                if (quotation is not null)
                {
                    quotation.TotalAmount = subtotal;
                    await _db.SaveChangesAsync();
                }
            }
            else
            {
                subtotal = await _db.Products
                    .Where(p => p.InvoiceId == invoiceId)
                    .SumAsync(p => p.TotalAmount);
            }

            return Ok(new { success = true, subtotal });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTaxRate(int id, int invoiceId, GstRate taxRate)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && p.InvoiceId == invoiceId);
            if (product is null)
            {
                return NotFound();
            }

            product.TaxRate = taxRate;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, taxRate = (int)product.TaxRate });
        }

        // Inline edit of a line item's Rate/Discount straight from the table on the
        // Products page. Pass invoiceId (invoice page) or quotationId (quotation
        // page) - exactly one - so the row is only ever changed from the document it
        // is being edited on.
        //
        // A line item billed from a quotation (InvoiceController.CreateFromQuotation)
        // is ONE Product row shared by the quotation and the invoice. Changing its
        // price from one side must not silently change the other document, so in that
        // case the edited document gets its own copy (with the new Rate/Discount) and
        // the shared row is left as it was for the other document. The new row's id
        // comes back as productId so the page can re-point that row at it.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLineItem(int id, int? invoiceId, int? quotationId, decimal? rate, decimal? discount)
        {
            if (invoiceId.HasValue == quotationId.HasValue)
            {
                return BadRequest(new { success = false, message = "Specify either an invoice or a quotation." });
            }

            var newRate = Math.Round(rate ?? 0m, 2, MidpointRounding.AwayFromZero);
            var newDiscount = Math.Round(discount ?? 0m, 2, MidpointRounding.AwayFromZero);

            if (newRate < 0 || newDiscount < 0)
            {
                return BadRequest(new { success = false, message = "Rate and Discount cannot be negative." });
            }

            var productQuery = _db.Products.Where(p => p.Id == id);
            productQuery = invoiceId.HasValue
                ? productQuery.Where(p => p.InvoiceId == invoiceId)
                : productQuery.Where(p => p.QuotationId == quotationId);

            var product = await productQuery.FirstOrDefaultAsync();
            if (product is null)
            {
                return NotFound();
            }

            if (!decimal.TryParse(product.Qty, NumberStyles.Number, CultureInfo.InvariantCulture, out var qtyValue))
            {
                qtyValue = 0m;
            }

            var newTotal = (qtyValue * newRate) - newDiscount;
            if (newTotal < 0)
            {
                return BadRequest(new { success = false, message = "Discount cannot be more than the line amount (Qty x Rate)." });
            }

            var target = product;
            if (product.InvoiceId.HasValue && product.QuotationId.HasValue)
            {
                target = new Product
                {
                    CustomerId = product.CustomerId,
                    InvoiceId = invoiceId,
                    QuotationId = quotationId,
                    Description = product.Description,
                    HsnSac = product.HsnSac,
                    Qty = product.Qty,
                    QtyType = product.QtyType,
                    TaxRate = product.TaxRate,
                };

                // Unlink the shared row from the document being edited only.
                if (invoiceId.HasValue)
                {
                    product.InvoiceId = null;
                }
                else
                {
                    product.QuotationId = null;
                }

                _db.Products.Add(target);
            }

            target.Rate = newRate;
            target.Discount = newDiscount;
            target.TotalAmount = newTotal;

            await _db.SaveChangesAsync();

            decimal subtotal;
            if (quotationId.HasValue)
            {
                subtotal = await _db.Products
                    .Where(p => p.QuotationId == quotationId)
                    .SumAsync(p => p.TotalAmount);

                var quotation = await _db.Quotations.FindAsync(quotationId.Value);
                if (quotation is not null)
                {
                    quotation.TotalAmount = subtotal;
                    await _db.SaveChangesAsync();
                }
            }
            else
            {
                subtotal = await _db.Products
                    .Where(p => p.InvoiceId == invoiceId)
                    .SumAsync(p => p.TotalAmount);
            }

            return Ok(new
            {
                success = true,
                productId = target.Id,
                rate = target.Rate,
                discount = target.Discount,
                totalAmount = target.TotalAmount,
                subtotal
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetAdditionalCost(int invoiceId, decimal? additionalCost, string? label)
        {
            var amount = Math.Round(additionalCost ?? 0m, 2, MidpointRounding.AwayFromZero);
            if (amount < 0)
            {
                return BadRequest(new { success = false, message = "Additional cost cannot be negative." });
            }

            label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
            if (label is not null && label.Length > 100)
            {
                return BadRequest(new { success = false, message = "Description cannot exceed 100 characters." });
            }

            var invoice = await _db.Invoices
                .Include(i => i.Products)
                .Include(i => i.Taxes)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice is null)
            {
                return NotFound();
            }

            var subtotal = invoice.Products.Sum(p => p.TotalAmount);
            var tax = invoice.Taxes.FirstOrDefault();

            // The Tax Summary on the Products page is only trusted while the invoice's
            // stored TotalAmount still matches what it was calculated from (see the
            // taxIsStale check in Views/Product/Index.cshtml). If it is still current,
            // keep it current after this change by moving TotalAmount along with the
            // new additional cost; if it's already stale, leave it stale.
            var taxSummaryWasCurrent = tax is not null
                && invoice.TotalAmount == subtotal + tax.TotalTax + invoice.AdditionalCost;

            invoice.AdditionalCost = amount;
            invoice.AdditionalCostLabel = amount > 0 ? label : null;

            if (taxSummaryWasCurrent)
            {
                invoice.TotalAmount = subtotal + tax!.TotalTax + amount;
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                additionalCost = invoice.AdditionalCost,
                additionalCostLabel = invoice.AdditionalCostLabel,
                subtotal,
                // Only set when the Tax Summary is still current, so the page can
                // refresh it in place; null means it stays hidden until Calculate Tax.
                grandTotal = taxSummaryWasCurrent ? invoice.TotalAmount : (decimal?)null
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CalculateTax(int invoiceId, decimal? totalTaxPercent)
        {
            var invoice = await _db.Invoices
                .Include(i => i.Products)
                .Include(i => i.Taxes)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice is null)
            {
                return NotFound();
            }

            if (!invoice.Products.Any())
            {
                return BadRequest(new { success = false, message = "Add at least one line item before calculating tax." });
            }

            var subtotal = invoice.Products.Sum(p => p.TotalAmount);

            decimal cgst, sgst, computedTotalTax;
            int? appliedTaxRatePercent = null;

            if (totalTaxPercent.HasValue && totalTaxPercent.Value > 0)
            {
                // A manually entered Total Tax % (from the Products page, next to
                // "Calculate Tax") overrides each line item's own Tax % - and, so the
                // Tax column actually reflects that instead of quietly staying at
                // whatever it was before (including 0%), every line item's TaxRate is
                // updated to match here too. GstRate's underlying values are whole
                // percentages, so the entered percent is rounded to the nearest whole
                // number for both storage and the calculation below (keeping the two
                // consistent with each other).
                appliedTaxRatePercent = (int)Math.Round(totalTaxPercent.Value, MidpointRounding.AwayFromZero);

                foreach (var product in invoice.Products)
                {
                    product.TaxRate = (GstRate)appliedTaxRatePercent.Value;
                }
            }

            cgst = 0;
            sgst = 0;
            foreach (var product in invoice.Products)
            {
                var halfRatePercent = (int)product.TaxRate / 2m;
                var itemTax = Math.Round(product.TotalAmount * halfRatePercent / 100m, 2);
                cgst += itemTax;
                sgst += itemTax;
            }

            computedTotalTax = cgst + sgst;

            // Additional cost (freight etc.) is added after tax and isn't itself taxed.
            var grandTotal = subtotal + computedTotalTax + invoice.AdditionalCost;

            var tax = invoice.Taxes.FirstOrDefault();
            if (tax is null)
            {
                tax = new Tax { CustomerId = invoice.CustomerId, InvoiceId = invoice.Id };
                _db.Taxes.Add(tax);
            }

            tax.Cgst = cgst;
            tax.Sgst = sgst;
            tax.TotalTax = computedTotalTax;

            invoice.TotalAmount = grandTotal;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                subtotal,
                cgst,
                sgst,
                totalTax = computedTotalTax,
                additionalCost = invoice.AdditionalCost,
                additionalCostLabel = invoice.AdditionalCostLabel,
                grandTotal,
                // Set only when the Total Tax % override was used - tells the page to
                // update every row's Tax column select to this value so it matches
                // what was just saved, without a full page reload.
                appliedTaxRatePercent
            });
        }
    }
}
