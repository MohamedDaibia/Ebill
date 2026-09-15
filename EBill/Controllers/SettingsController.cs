using System.Security.Claims;
using EBill.Data;
using EBill.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EBill.Controllers
{
    // Status page for the "Connect" flow's other direction: Stock Management is the
    // one that initiates the connection (see EbillConnectService/AccountController
    // .Login/CompanyController.Register and, for the API link back, StockSync
    // Controller), so there's nothing to click here - it just shows whether a Stock
    // Management company is currently linked to this Ebill company, and whether
    // Ebill can call back into its API yet.
    //
    // Also hosts the document auto-numbering settings (UpdateNumbering below) -
    // unrelated to the Stock Management connection, just kept on the same page.
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public SettingsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var companyIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(companyIdClaim, out var companyId))
            {
                return Unauthorized();
            }

            var settings = await _db.Settings.FirstOrDefaultAsync(s => s.CompanyId == companyId);

            return View(settings ?? new Settings { CompanyId = companyId });
        }

        // Saves the "next number" counters for Invoice/Quotation/Purchase Order
        // numbering. Takes plain scalar parameters rather than binding the whole
        // Settings entity, so a posted form can never touch StockCompanyId/
        // RefreshToken/IsSynced - those are only ever set by the Stock Management
        // connect flow. A blank field leaves that counter untouched rather than
        // clearing it, so re-saving one number doesn't wipe the others.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateNumbering(int? nextInvoiceNumber, int? nextQuotationNumber, int? nextPurchaseOrderNumber)
        {
            var companyIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(companyIdClaim, out var companyId))
            {
                return Unauthorized();
            }

            if ((nextInvoiceNumber.HasValue && nextInvoiceNumber.Value < 1)
                || (nextQuotationNumber.HasValue && nextQuotationNumber.Value < 1)
                || (nextPurchaseOrderNumber.HasValue && nextPurchaseOrderNumber.Value < 1))
            {
                TempData["NumberingError"] = "Numbers must be positive.";
                return RedirectToAction(nameof(Index));
            }

            var settings = await _db.Settings.FirstOrDefaultAsync(s => s.CompanyId == companyId);
            if (settings is null)
            {
                settings = new Settings { CompanyId = companyId };
                _db.Settings.Add(settings);
            }

            if (nextInvoiceNumber.HasValue)
            {
                settings.NextInvoiceNumber = nextInvoiceNumber.Value;
            }

            if (nextQuotationNumber.HasValue)
            {
                settings.NextQuotationNumber = nextQuotationNumber.Value;
            }

            if (nextPurchaseOrderNumber.HasValue)
            {
                settings.NextPurchaseOrderNumber = nextPurchaseOrderNumber.Value;
            }

            await _db.SaveChangesAsync();

            TempData["NumberingSaved"] = true;
            return RedirectToAction(nameof(Index));
        }
    }
}
