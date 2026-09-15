using System.Security.Claims;
using EBill.Data;
using EBill.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EBill.Controllers
{
    [Authorize]
    public class QuotationController : Controller
    {
        private readonly ApplicationDbContext _db;

        public QuotationController(ApplicationDbContext db)
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

            // Same reuse-by-phone logic as InvoiceController.Create - see the comment
            // there. Also fixes a pre-existing gap here: this action wasn't setting
            // CompanyId at all before, so quotation-created customers weren't scoped
            // to the company like everywhere else.
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

            var quotation = new Quotation
            {
                CustomerId = customer.Id,
                Date = DateTime.UtcNow,
            };

            var settings = await _db.Settings.FirstOrDefaultAsync(s => s.CompanyId == companyId);
            if (settings?.NextQuotationNumber is int nextNumber)
            {
                quotation.QuotationNumber = nextNumber.ToString();
                settings.NextQuotationNumber = nextNumber + 1;
            }

            _db.Quotations.Add(quotation);
            await _db.SaveChangesAsync();

            return RedirectToAction("QuotationIndex", "Product", new { quotationId = quotation.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? days)
        {
            var query = _db.Quotations
                .Include(q => q.Customer)
                .Include(q => q.Products)
                .AsQueryable();

            if (days.HasValue && days.Value > 0)
            {
                var fromDate = DateTime.UtcNow.Date.AddDays(-(days.Value - 1));
                query = query.Where(q => q.Date >= fromDate);
            }

            var quotations = await query
                .OrderByDescending(q => q.Date)
                .ThenByDescending(q => q.Id)
                .ToListAsync();

            ViewData["SelectedDays"] = days;
            return View(quotations);
        }

        [HttpGet]
        public async Task<IActionResult> View(int id)
        {
            var quotation = await _db.Quotations
                .Include(q => q.Customer)
                .Include(q => q.Products)
                .Include(q => q.Invoices)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quotation is null)
            {
                return NotFound();
            }

            Company? company = null;
            var companyIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(companyIdClaim, out var companyId))
            {
                company = await _db.Companies.FindAsync(companyId);
            }

            ViewData["Company"] = company;

            // "Next"/"Previous" step through quotations in creation order (by Id).
            ViewData["PreviousQuotationId"] = await _db.Quotations
                .Where(q => q.Id < id)
                .OrderByDescending(q => q.Id)
                .Select(q => (int?)q.Id)
                .FirstOrDefaultAsync();

            ViewData["NextQuotationId"] = await _db.Quotations
                .Where(q => q.Id > id)
                .OrderBy(q => q.Id)
                .Select(q => (int?)q.Id)
                .FirstOrDefaultAsync();

            return View(quotation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int? days)
        {
            var quotation = await _db.Quotations
                .Include(q => q.Products)
                .Include(q => q.Invoices)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quotation is null)
            {
                return NotFound();
            }

            if (quotation.Invoices.Any())
            {
                // Invoices reference this quotation with DeleteBehavior.Restrict, so
                // it can't be removed while an invoice has already been billed from it.
                return BadRequest(new { success = false, message = "Cannot delete a quotation that already has an invoice created from it." });
            }

            // Products are configured with DeleteBehavior.Restrict, so they must
            // be removed explicitly before the quotation itself can be deleted.
            _db.Products.RemoveRange(quotation.Products);
            _db.Quotations.Remove(quotation);

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { days });
        }
    }
}
