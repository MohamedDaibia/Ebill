using System.Security.Claims;
using EBill.Data;
using EBill.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EBill.Controllers
{
    // API-style controller used server-to-server by Stock Management, authenticated
    // with the access token issued by TokenController.Refresh (see that controller for
    // how the Bearer token is obtained from a stored refresh token).
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public CustomerController(ApplicationDbContext db)
        {
            _db = db;
        }

        // Backs Stock Management's Dispatch List / Stock List "Dispatch To" / "Received
        // From" autocomplete (see StockManagement's SettingsController.SearchEbillCustomers).
        // Scoped to the calling company (from the access token's claims) now that
        // Customer.CompanyId exists - a company only ever sees its own customers.
        [HttpGet("Search")]
        public async Task<IActionResult> Search([FromQuery] string? query, [FromQuery] int limit = 10)
        {
            var companyIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(companyIdClaim, out var companyId))
            {
                return Unauthorized();
            }

            var clampedLimit = Math.Clamp(limit, 1, 25);

            var customersQuery = _db.Customers.Where(c => c.CompanyId == companyId);

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim();
                customersQuery = customersQuery.Where(c => c.Name.Contains(term));
            }

            var results = await customersQuery
                .OrderBy(c => c.Name)
                .Take(clampedLimit)
                .Select(c => new CustomerSearchResultDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    PhoneNumber = c.PhoneNumber
                })
                .ToListAsync();

            return Ok(results);
        }

        // Backs the phone-number auto-fill on the Create Invoice / Create Quotation
        // pages: once a full 10-digit mobile number is typed in, this looks up whether
        // a customer with that number already exists for the company and, if so,
        // returns their Name/Address/GSTIN so the rest of the form fills itself in.
        // Phone numbers are stored however they were typed (with an optional "+91"
        // prefix and spaces/dashes per the Customer.PhoneNumber format), so matching
        // is done on the normalized (digits-only, country-code-stripped) number rather
        // than a literal string comparison.
        [HttpGet("LookupByPhone")]
        public async Task<IActionResult> LookupByPhone([FromQuery] string? phone)
        {
            var companyIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(companyIdClaim, out var companyId))
            {
                return Unauthorized();
            }

            var normalizedQuery = NormalizePhone(phone);
            if (normalizedQuery is null)
            {
                return Ok((CustomerLookupResultDto?)null);
            }

            // Normalizing PhoneNumber isn't translatable to SQL, so the comparison is
            // done in memory - fine at the scale of one company's customer list.
            var companyCustomers = await _db.Customers
                .Where(c => c.CompanyId == companyId)
                .Select(c => new { c.Id, c.Name, c.Address, c.PhoneNumber, c.Gstin })
                .ToListAsync();

            var match = companyCustomers.FirstOrDefault(c => NormalizePhone(c.PhoneNumber) == normalizedQuery);

            if (match is null)
            {
                return Ok((CustomerLookupResultDto?)null);
            }

            return Ok(new CustomerLookupResultDto
            {
                Id = match.Id,
                Name = match.Name,
                Address = match.Address,
                PhoneNumber = match.PhoneNumber,
                Gstin = match.Gstin
            });
        }

        // Strips everything but digits and, if a "91" country code is present ahead of
        // a 10-digit number, drops it - so "+91 98765-43210", "9198765 43210" and
        // "9876543210" all normalize to the same "9876543210".
        private static string? NormalizePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return null;
            }

            var digits = new string(phone.Where(char.IsDigit).ToArray());

            if (digits.Length == 12 && digits.StartsWith("91"))
            {
                digits = digits.Substring(2);
            }

            return digits.Length == 10 ? digits : null;
        }
    }
}
