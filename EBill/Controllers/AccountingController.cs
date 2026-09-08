using EBill.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EBill.Controllers
{
    // Attribute-routed (rather than the default {controller}/{action}/{id} convention)
    // so the shareable link is exactly "/Accounting/{guid}" with no action segment.
    // Deliberately NOT [Authorize]: this is a shareable report link (e.g. for an
    // accountant without a login) — the GUID itself is the access control, so treat
    // it like a secret. Anyone with the link can view the invoices it covers.
    [Route("Accounting")]
    public class AccountingController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AccountingController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Index(Guid id)
        {
            var link = await _db.AccountingLinks.FindAsync(id);

            if (link is null)
            {
                return NotFound();
            }

            var invoices = await _db.Invoices
                .Include(i => i.Products)
                .Include(i => i.Taxes)
                .Where(i => i.Date >= link.FromDate && i.Date <= link.ToDate)
                .OrderByDescending(i => i.Date)
                .ThenByDescending(i => i.Id)
                .ToListAsync();

            ViewData["FromDate"] = link.FromDate;
            ViewData["ToDate"] = link.ToDate;
            ViewData["Days"] = link.Days;
            ViewData["LinkId"] = id;

            return View(invoices);
        }

        [HttpGet("{id:guid}/invoice/{invoiceId:int}")]
        public async Task<IActionResult> Invoice(Guid id, int invoiceId)
        {
            var link = await _db.AccountingLinks.FindAsync(id);
            if (link is null)
            {
                return NotFound();
            }

            var invoice = await _db.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Products)
                .Include(i => i.Taxes)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            // Only invoices that fall inside this link's own date range are reachable
            // here, so a valid link can't be used to browse arbitrary invoices by
            // guessing invoice IDs.
            if (invoice is null || invoice.Date < link.FromDate || invoice.Date > link.ToDate)
            {
                return NotFound();
            }

            // Single-tenant app: there is exactly one Company row for the whole deployment.
            var company = await _db.Companies.FirstOrDefaultAsync();

            ViewData["Company"] = company;
            ViewData["ReadOnly"] = true;
            ViewData["AccountingLinkId"] = id;

            // Previous/Next stay inside this link's own date range, same security
            // boundary as the lookup above — no stepping into invoices outside it.
            ViewData["PreviousInvoiceId"] = await _db.Invoices
                .Where(i => i.Id < invoiceId && i.Date >= link.FromDate && i.Date <= link.ToDate)
                .OrderByDescending(i => i.Id)
                .Select(i => (int?)i.Id)
                .FirstOrDefaultAsync();

            ViewData["NextInvoiceId"] = await _db.Invoices
                .Where(i => i.Id > invoiceId && i.Date >= link.FromDate && i.Date <= link.ToDate)
                .OrderBy(i => i.Id)
                .Select(i => (int?)i.Id)
                .FirstOrDefaultAsync();

            return View("~/Views/Invoice/View.cshtml", invoice);
        }
    }
}
