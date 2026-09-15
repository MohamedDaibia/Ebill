using System.Security.Cryptography;
using System.Text;
using EBill.Data;
using EBill.Models;
using EBill.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EBill.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly JwtTokenService _tokenService;

        public TokenController(ApplicationDbContext db, JwtTokenService tokenService)
        {
            _db = db;
            _tokenService = tokenService;
        }

        // Exchanges a company's stored refresh token for a fresh Ebill access token.
        // Called server-to-server by Stock Management's EbillApiClient — no interactive
        // login involved. No [Authorize]: the refresh token itself is the credential.
        [HttpPost("Refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request)
        {
            if (request == null
                || string.IsNullOrWhiteSpace(request.EbillCompanyId)
                || string.IsNullOrWhiteSpace(request.RefreshToken)
                || !int.TryParse(request.EbillCompanyId, out var companyId))
            {
                return BadRequest(new { message = "A valid EbillCompanyId and RefreshToken are required." });
            }

            var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
            if (company == null || string.IsNullOrEmpty(company.RefreshTokenHash))
            {
                return Unauthorized(new { message = "Invalid refresh token." });
            }
            if (company.RefreshTokenExpiresAt == null || company.RefreshTokenExpiresAt < DateTime.UtcNow)
            {
                return Unauthorized(new { message = "Refresh token expired — reconnect via Stock Management's Connect E-bill button." });
            }

            var presentedHash = RefreshTokenGenerator.Hash(request.RefreshToken);
            if (!TokensMatch(presentedHash, company.RefreshTokenHash))
            {
                return Unauthorized(new { message = "Invalid refresh token." });
            }

            var accessToken = _tokenService.GenerateToken(company);
            return Ok(new { accessToken, ebillCompanyId = company.Id, companyName = company.CompanyName });
        }

        private static bool TokensMatch(string a, string b)
        {
            var aBytes = Encoding.UTF8.GetBytes(a);
            var bBytes = Encoding.UTF8.GetBytes(b);
            return aBytes.Length == bBytes.Length && CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
        }
    }
}
