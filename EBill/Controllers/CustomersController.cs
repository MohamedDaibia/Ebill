using System.Security.Claims;
using EBill.Data;
using EBill.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EBill.Controllers
{
    // Razor-view controller for the "Customers" nav link - lists, adds, edits and
    // deletes a company's own customers. Deliberately named CustomersController
    // (plural), distinct from the existing api/Customer/Search endpoint
    // (Controllers/CustomerController.cs, used server-to-server by Stock Management) -
    // same underlying Customer data, two different consumers.
    [Authorize(Roles = UserRoles.Admin)]
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CustomersController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var companyId = GetCompanyId();
            if (companyId is null)
            {
                return Unauthorized();
            }

            var customers = await _db.Customers
                .Where(c => c.CompanyId == companyId)
                .OrderBy(c => c.Name)
                .ToListAsync();

            // Drives whether the "Create Invoice" button shows on each row - only
            // once this Ebill company is actually connected to Stock Management
            // (inventory), same condition Settings/Index.cshtml uses for its
            // "Connected" state.
            var settings = await _db.Settings.FirstOrDefaultAsync(s => s.CompanyId == companyId);
            ViewData["InventoryConnected"] = settings?.IsSynced ?? false;

            return View(customers);
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
            var companyId = GetCompanyId();
            if (companyId is null)
            {
                return Unauthorized();
            }

            model.CompanyId = companyId.Value;

            if (await _db.Customers.AnyAsync(c => c.CompanyId == companyId && c.PhoneNumber == model.PhoneNumber))
            {
                ModelState.AddModelError(nameof(model.PhoneNumber), "A customer with this phone number already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _db.Customers.Add(model);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var companyId = GetCompanyId();
            if (companyId is null)
            {
                return Unauthorized();
            }

            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);
            if (customer is null)
            {
                return NotFound();
            }

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Customer model)
        {
            var companyId = GetCompanyId();
            if (companyId is null)
            {
                return Unauthorized();
            }

            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);
            if (customer is null)
            {
                return NotFound();
            }

            if (await _db.Customers.AnyAsync(c => c.CompanyId == companyId && c.PhoneNumber == model.PhoneNumber && c.Id != id))
            {
                ModelState.AddModelError(nameof(model.PhoneNumber), "A customer with this phone number already exists.");
            }

            if (!ModelState.IsValid)
            {
                // model came straight from the posted form, so the fields the form
                // doesn't include (Id/CompanyId/CustomerNumber) need restoring from the
                // tracked entity before redisplaying - otherwise the form would post
                // back to the wrong URL and the page subheading would go blank.
                model.Id = id;
                model.CompanyId = companyId.Value;
                model.CustomerNumber = customer.CustomerNumber;
                return View(model);
            }

            customer.Name = model.Name;
            customer.PhoneNumber = model.PhoneNumber;
            customer.Address = model.Address;
            customer.State = model.State;
            customer.District = model.District;
            customer.Gstin = model.Gstin;

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var companyId = GetCompanyId();
            if (companyId is null)
            {
                return Unauthorized();
            }

            var customer = await _db.Customers
                .Include(c => c.Products)
                .Include(c => c.Invoices)
                .Include(c => c.Quotations)
                .Include(c => c.Taxes)
                .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);

            if (customer is null)
            {
                return NotFound();
            }

            // Products/Invoices/Quotations/Taxes all reference Customer with
            // DeleteBehavior.Restrict (see ApplicationDbContext), so a customer that
            // already has any of these can't be deleted outright - same reasoning
            // QuotationController.Delete uses for quotations with invoices already
            // billed from them. Unlike that action (a modal form submitted as a plain
            // POST, where a JSON error response would just print raw JSON to the page),
            // this uses TempData + a redirect so the error renders as a normal page
            // alert on Index.
            if (customer.Invoices.Any() || customer.Quotations.Any() || customer.Products.Any() || customer.Taxes.Any())
            {
                TempData["DeleteError"] = $"Cannot delete {customer.Name} - this customer already has invoices, quotations, or line items on file.";
                return RedirectToAction(nameof(Index));
            }

            _db.Customers.Remove(customer);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private int? GetCompanyId()
        {
            var companyIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(companyIdClaim, out var companyId) ? companyId : null;
        }
    }
}
