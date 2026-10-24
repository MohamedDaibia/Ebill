using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EBill.Models;
using Microsoft.IdentityModel.Tokens;

namespace EBill.Services
{
    public class JwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Company owner logs in as Admin; a sub-user (AppUser) carries their own role/name.
        public string GenerateToken(Company company, AppUser? user = null)
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, company.Id.ToString()),
                new Claim(ClaimTypes.Email, user?.Email ?? company.Email),
                new Claim(ClaimTypes.Name, user?.Name ?? company.CompanyName),
                new Claim(ClaimTypes.Role, user?.Role ?? UserRoles.Admin),
            };

            var expiresMinutes = int.Parse(jwtSection["ExpiresMinutes"] ?? "60");

            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
