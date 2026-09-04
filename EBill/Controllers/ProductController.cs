using System.Globalization;
using EBill.Data;
using EBill.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EBill.Controllers
{
    [Authorize]
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
        public async Task<IActionResult> Remove(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product is null)
            {
                return NotFound();
            }

            var invoiceId = product.InvoiceId;

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();

            var subtotal = await _db.Products
                .Where(p => p.InvoiceId == invoiceId)
                .SumAsync(p => p.TotalAmount);

            return Ok(new { success = true, subtotal });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CalculateTax(int invoiceId)
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

            decimal cgst = 0, sgst = 0;
            foreach (var product in invoice.Products)
            {
                var halfRatePercent = (int)product.TaxRate / 2m;
                var itemTax = Math.Round(product.TotalAmount * halfRatePercent / 100m, 2);
                cgst += itemTax;
                sgst += itemTax;
            }

            var totalTax = cgst + sgst;
            var grandTotal = subtotal + totalTax;

            var tax = invoice.Taxes.FirstOrDefault();
            if (tax is null)
            {
                tax = new Tax { CustomerId = invoice.CustomerId, InvoiceId = invoice.Id };
                _db.Taxes.Add(tax);
            }

            tax.Cgst = cgst;
            tax.Sgst = sgst;
            tax.TotalTax = totalTax;

            invoice.TotalAmount = grandTotal;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                subtotal,
                cgst,
                sgst,
                totalTax,
                grandTotal
            });
        }
    }
}
