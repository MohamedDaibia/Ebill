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
