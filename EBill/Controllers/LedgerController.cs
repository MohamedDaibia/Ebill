using System.Security.Claims;
using System.Text.RegularExpressions;
using EBill.Data;
using EBill.Helpers;
using EBill.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EBill.Controllers
{
    // Open to every role. Sales/Accountant users land here straight after login and
    // can reach nothing else; Admin gets here from the "Ledger" card.
    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Accountant + "," + UserRoles.Sales)]
    public class LedgerController : Controller
    {
        private readonly ApplicationDbContext _db;

        public LedgerController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? q)
        {
            var companyId = GetCompanyId();
            if (companyId is null)
            {
                return Unauthorized();
            }

            // Case-insensitive "contains" search across name, address, district and state.
            var term = q?.Trim();
            var customers = _db.Customers.AsNoTracking().Where(c => c.CompanyId == companyId);
            if (!string.IsNullOrEmpty(term))
            {
                customers = customers.Where(c =>
                    c.Name.Contains(term) ||
                    (c.Address != null && c.Address.Contains(term)) ||
                    (c.District != null && c.District.Contains(term)) ||
                    (c.State != null && c.State.Contains(term)));
            }
            ViewData["Query"] = term;

            // Every matching customer of the company, left-joined to their LedgerBalance row
            // (customers with no ledger entries yet show 0.00 across the board).
            var rows = await (
                from c in customers
                join b in _db.LedgerBalances.AsNoTracking().Where(b => b.CompanyId == companyId)
                    on c.Id equals b.CustomerId into balances
                from b in balances.DefaultIfEmpty()
                orderby c.Name
                select new LedgerCustomerRow
                {
                    CustomerId = c.Id,
                    Name = c.Name,
                    Address = c.Address,
                    District = c.District,
                    State = c.State,
                    TotalCredited = b == null ? 0m : b.TotalCredited,
                    TotalDebited = b == null ? 0m : b.TotalDebited,
                    Balance = b == null ? 0m : b.TotalRemaining
                }).ToListAsync();

            return View(rows);
        }

        // Customer Details: totals, ledger entries, "Add Debit" form and the map.
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var companyId = GetCompanyId();
            if (companyId is null)
            {
                return Unauthorized();
            }

            var customer = await _db.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);
            if (customer is null)
            {
                return NotFound();
            }

            var balance = await _db.LedgerBalances.AsNoTracking()
                .FirstOrDefaultAsync(b => b.CompanyId == companyId && b.CustomerId == id);

            var entries = await _db.LedgerDetails.AsNoTracking()
                .Where(l => l.CompanyId == companyId && l.CustomerId == id)
                .OrderByDescending(l => l.CreatedAt)
                .Take(200)
                .ToListAsync();

            // This customer's invoices: feeds the dropdown and the Invoice column.
            var invoices = await _db.Invoices.AsNoTracking()
                .Where(i => i.CustomerId == id)
                .OrderByDescending(i => i.Date)
                .ThenByDescending(i => i.Id)
                .ToListAsync();
            var invoiceNumbers = invoices.ToDictionary(i => i.Id, i => i.DisplayBillNumber);

            var model = new LedgerCustomerDetailsViewModel
            {
                CustomerId = customer.Id,
                CustomerNumber = customer.CustomerNumber,
                Name = customer.Name,
                PhoneNumber = customer.PhoneNumber,
                Gstin = customer.Gstin,
                Address = customer.Address,
                District = customer.District,
                State = customer.State,
                TotalCredited = balance?.TotalCredited ?? 0m,
                TotalDebited = balance?.TotalDebited ?? 0m,
                TotalBalance = balance?.TotalRemaining ?? 0m,
                SavedMapLink = customer.MapLink,
                Invoices = invoices.Select(i => new InvoiceOption
                {
                    Id = i.Id,
                    Label = $"{i.DisplayBillNumber} - ₹{MoneyFormat.Format(i.TotalAmount)}"
                }).ToList(),
                Entries = entries.Select(l =>
                {
                    var isDebit = l.DebitedAmount > 0;
                    return new LedgerEntryRow
                    {
                        Id = l.Id,
                        InvoiceId = l.InvoiceId,
                        Date = ToIndiaTime(l.CreatedAt),
                        IsDebit = isDebit,
                        Comments = l.Comments,
                        By = isDebit ? l.DebitedBy : l.CreditedBy,
                        PayBy = l.PayBy,
                        InvoiceNumber = l.InvoiceId is int invId && invoiceNumbers.TryGetValue(invId, out var num) ? num : null,
                        Credited = l.CreditedAmount,
                        Debited = l.DebitedAmount
                    };
                }).ToList()
            };

            var place = string.Join(", ", new[] { customer.Address, customer.District, customer.State }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
            if (!string.IsNullOrEmpty(place))
            {
                var encoded = Uri.EscapeDataString(place);
                model.MapOpenUrl = $"https://www.google.com/maps/search/?api=1&query={encoded}";
                model.MapEmbedUrl = !string.IsNullOrWhiteSpace(customer.MapLink)
                    ? customer.MapLink
                    : $"https://www.google.com/maps?q={encoded}&output=embed";
            }
            else if (!string.IsNullOrWhiteSpace(customer.MapLink))
            {
                model.MapEmbedUrl = customer.MapLink;
            }

            return View(model);
        }

        // Sales / employee records an amount debited against the customer, with a comment.
        // Writes the LedgerDetails row and updates the customer's LedgerBalance together.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDebit(int id, decimal debitAmount, string? comments, string? payBy, int? invoiceId)
        {
            var companyId = GetCompanyId();
            if (companyId is null)
            {
                return Unauthorized();
            }

            var customerName = await _db.Customers
                .Where(c => c.Id == id && c.CompanyId == companyId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync();
            if (customerName is null)
            {
                return NotFound();
            }

            var (error, cleanComments, cleanPayBy, cleanAmount) = await ValidateDebitInputAsync(id, debitAmount, comments, payBy, invoiceId);
            if (error is not null)
            {
                TempData["LedgerError"] = error;
                return RedirectToAction(nameof(Details), new { id });
            }
            comments = cleanComments;
            payBy = cleanPayBy;
            debitAmount = cleanAmount;

            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
            var now = DateTime.UtcNow;

            await using var tx = await _db.Database.BeginTransactionAsync();

            _db.LedgerDetails.Add(new LedgerDetails
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId.Value,
                CustomerId = id,
                DebitedAmount = debitAmount,
                DebitedBy = customerName,   // shown as "Debited By"; CreatedBy still records the logged-in user
                Comments = comments,
                PayBy = payBy,
                InvoiceId = invoiceId,
                CreatedAt = now,
                CreatedBy = userName
            });

            var balance = await _db.LedgerBalances
                .FirstOrDefaultAsync(b => b.CompanyId == companyId && b.CustomerId == id);
            if (balance is null)
            {
                balance = new LedgerBalance
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId.Value,
                    CustomerId = id,
                    CreatedAt = now,
                    CreatedBy = userName
                };
                _db.LedgerBalances.Add(balance);
            }
            else
            {
                balance.UpdatedAt = now;
                balance.UpdatedBy = userName;
            }

            balance.TotalDebited += debitAmount;
            balance.TotalRemaining = balance.TotalCredited - balance.TotalDebited;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["LedgerSuccess"] = $"Debit of ₹{debitAmount:N2} added.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Edit an existing debit (amount, invoice, pay by, comments). Credits - including the
        // automatic ones created by invoices - can't be edited here. The customer's
        // LedgerBalance moves by the change in amount.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDebit(int id, Guid entryId, decimal debitAmount, string? comments, string? payBy, int? invoiceId)
        {
            var companyId = GetCompanyId();
            if (companyId is null)
            {
                return Unauthorized();
            }

            var entry = await _db.LedgerDetails.FirstOrDefaultAsync(l =>
                l.Id == entryId && l.CustomerId == id && l.CompanyId == companyId && l.DebitedAmount > 0);
            if (entry is null)
            {
                return NotFound();
            }

            var (error, cleanComments, cleanPayBy, cleanAmount) = await ValidateDebitInputAsync(id, debitAmount, comments, payBy, invoiceId);
            if (error is not null)
            {
                TempData["LedgerError"] = error;
                return RedirectToAction(nameof(Details), new { id });
            }

            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
            var now = DateTime.UtcNow;
            var delta = cleanAmount - entry.DebitedAmount;

            await using var tx = await _db.Database.BeginTransactionAsync();

            entry.DebitedAmount = cleanAmount;
            entry.Comments = cleanComments;
            entry.PayBy = cleanPayBy;
            entry.InvoiceId = invoiceId;
            entry.UpdatedAt = now;
            entry.UpdatedBy = userName;

            if (delta != 0)
            {
                var balance = await _db.LedgerBalances
                    .FirstOrDefaultAsync(b => b.CompanyId == companyId && b.CustomerId == id);
                if (balance is null)
                {
                    balance = new LedgerBalance
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = companyId.Value,
                        CustomerId = id,
                        CreatedAt = now,
                        CreatedBy = userName
                    };
                    _db.LedgerBalances.Add(balance);
                }
                else
                {
                    balance.UpdatedAt = now;
                    balance.UpdatedBy = userName;
                }

                balance.TotalDebited += delta;
                balance.TotalRemaining = balance.TotalCredited - balance.TotalDebited;
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["LedgerSuccess"] = "Debit updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Shared checks for adding / editing a debit. Returns an error message, or the
        // cleaned-up values (trimmed comments, defaulted Pay By, amount rounded to paise).
        private async Task<(string? Error, string? Comments, string? PayBy, decimal Amount)> ValidateDebitInputAsync(
            int customerId, decimal debitAmount, string? comments, string? payBy, int? invoiceId)
        {
            comments = comments?.Trim();
            payBy = string.IsNullOrWhiteSpace(payBy) ? PaymentModes.Cash : payBy.Trim();
            debitAmount = Math.Round(debitAmount, 2);

            if (debitAmount <= 0 || debitAmount > 9_999_999_999m)
                return ("Enter a debit amount greater than zero.", null, null, 0);
            if (string.IsNullOrEmpty(comments))
                return ("Comments are required.", null, null, 0);
            if (comments.Length > 500)
                return ("Comments cannot exceed 500 characters.", null, null, 0);
            if (!PaymentModes.All.Contains(payBy))
                return ("Choose a valid Pay By option.", null, null, 0);
            if (invoiceId is not null &&
                !await _db.Invoices.AnyAsync(i => i.Id == invoiceId && i.CustomerId == customerId))
                return ("That invoice doesn't belong to this customer.", null, null, 0);

            return (null, comments, payBy, debitAmount);
        }

        // Admin only: saves (or clears) the Google Maps embed link for a customer.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> SaveMap(int id, string? mapLink)
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

            var input = mapLink?.Trim();
            if (string.IsNullOrEmpty(input))
            {
                customer.MapLink = null;
            }
            else
            {
                var url = ExtractGoogleMapsEmbedUrl(input);
                if (url is null)
                {
                    TempData["LedgerError"] = "Paste a Google Maps embed link (Share > Embed a map), or its iframe code.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                customer.MapLink = url;
            }

            await _db.SaveChangesAsync();
            TempData["LedgerSuccess"] = "Map saved.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Accepts either the embed URL itself or the whole <iframe ...> snippet Google
        // gives you, and only lets through https Google Maps URLs.
        private static string? ExtractGoogleMapsEmbedUrl(string input)
        {
            if (input.Contains("<iframe", StringComparison.OrdinalIgnoreCase))
            {
                var m = Regex.Match(input, "src\\s*=\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);
                if (!m.Success) return null;
                input = m.Groups[1].Value;
            }

            input = System.Net.WebUtility.HtmlDecode(input);
            if (input.Length > 1000) return null;
            if (!Uri.TryCreate(input, UriKind.Absolute, out var uri)) return null;
            if (uri.Scheme != Uri.UriSchemeHttps) return null;
            if (uri.Host != "www.google.com" && uri.Host != "maps.google.com" && uri.Host != "google.com") return null;
            if (!uri.AbsolutePath.StartsWith("/maps", StringComparison.OrdinalIgnoreCase)) return null;
            return uri.ToString();
        }

        private static DateTime ToIndiaTime(DateTime utc)
        {
            var asUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(asUtc, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
        }

        private int? GetCompanyId()
        {
            var companyIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(companyIdClaim, out var companyId) ? companyId : null;
        }
    }
}
