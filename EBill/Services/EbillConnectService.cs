using EBill.Data;
using EBill.Models;

namespace EBill.Services
{
    // Backs the "Connect" flow used by external apps (e.g. Stock Management's
    // Settings page) that send a user here to log in or register, then need this
    // company's Ebill id plus a refresh token handed back via redirect.
    public class EbillConnectService
    {
        private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(90);

        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;

        public EbillConnectService(ApplicationDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        // Guards against an attacker crafting a returnUrl that points a company's
        // Ebill refresh token at an origin that isn't actually Stock Management (or
        // whatever else is explicitly allowlisted in appsettings' AllowedConnectOrigins).
        public bool IsAllowedReturnUrl(string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl) || !Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
            {
                return false;
            }

            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            {
                return false;
            }

            var origin = $"{uri.Scheme}://{uri.Authority}";
            var allowedOrigins = _configuration.GetSection("AllowedConnectOrigins").Get<string[]>() ?? Array.Empty<string>();

            return allowedOrigins.Any(allowed =>
                string.Equals(allowed.TrimEnd('/'), origin, StringComparison.OrdinalIgnoreCase));
        }

        // Issues a fresh refresh token for `company` (persisting only its hash on
        // the company record) and appends it, plus the company's Ebill id, to
        // returnUrl as query params for the caller to pick up.
        public async Task<string> IssueTokenAndBuildRedirectUrlAsync(Company company, string returnUrl)
        {
            var (plainText, hash) = RefreshTokenGenerator.Generate();

            company.RefreshTokenHash = hash;
            company.RefreshTokenExpiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime);
            await _db.SaveChangesAsync();

            var separator = returnUrl.Contains('?') ? "&" : "?";
            return $"{returnUrl}{separator}ebillCompanyId={Uri.EscapeDataString(company.Id.ToString())}&refreshToken={Uri.EscapeDataString(plainText)}";
        }
    }
}
