using System.Security.Claims;
using EBill.Data;
using EBill.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EBill.Controllers
{
    // Called server-to-server by Stock Management, authenticated with the Ebill
    // access token it just redeemed via TokenController.Refresh (same pattern as
    // CustomerController.Search - Bearer token, company id read from claims, not a
    // request body field, so a caller can only register a link for its own company).
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class StockSyncController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public StockSyncController(ApplicationDbContext db)
        {
            _db = db;
        }

        // Hands Ebill the refresh token it needs to call Stock Management's own API.
        // Stock Management calls this itself immediately after its ConnectEbill
        // succeeds (see that controller) - the browser-redirect "connect" handshake
        // only tells Ebill Stock Management's company id (see
        // EbillConnectService.IssueTokenAndBuildRedirectUrlAsync); this is the second
        // half that actually makes the link usable in both directions.
        [HttpPost("RegisterRefreshToken")]
        public async Task<IActionResult> RegisterRefreshToken([FromBody] StockConnectRequestDto request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.StockCompanyId) || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest(new { message = "StockCompanyId and RefreshToken are required." });
            }

            var companyIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(companyIdClaim, out var companyId))
            {
                return Unauthorized();
            }

            var settings = await _db.Settings.FirstOrDefaultAsync(s => s.CompanyId == companyId);
            if (settings is null)
            {
                settings = new Settings { CompanyId = companyId };
                _db.Settings.Add(settings);
            }

            settings.StockCompanyId = request.StockCompanyId;
            settings.RefreshToken = request.RefreshToken;
            settings.IsSynced = true;

            await _db.SaveChangesAsync();

            return Ok();
        }
    }
}
