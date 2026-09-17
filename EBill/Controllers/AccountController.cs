using EBill.Data;
using EBill.Models;
using EBill.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EBill.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IPasswordHasher<Company> _passwordHasher;
        private readonly JwtTokenService _tokenService;
        private readonly EbillConnectService _connectService;
        private readonly IEmailSender _emailSender;

        public AccountController(
            ApplicationDbContext db,
            IPasswordHasher<Company> passwordHasher,
            JwtTokenService tokenService,
            EbillConnectService connectService,
            IEmailSender emailSender)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _connectService = connectService;
            _emailSender = emailSender;
        }

        // returnUrl is set when another app (e.g. Stock Management's "Sync Ebill"
        // button) sent the user here to connect — only kept if it's on the
        // AllowedConnectOrigins allowlist, otherwise treated as if absent.
        // stockCompanyId (the connecting app's own company id) rides along the same
        // way, and is dropped whenever returnUrl itself isn't trusted.
        [HttpGet]
        public IActionResult Login(string? returnUrl = null, string? stockCompanyId = null)
        {
            // A stale AuthToken cookie left over from an earlier session makes this
            // GET request look "authenticated" to the antiforgery system, which then
            // binds the token it generates here to that identity. That identity isn't
            // guaranteed to still match by the time the form POSTs back, which fails
            // validation with "meant for a different claims-based user". Clearing the
            // cookie and redirecting back to this same action makes the page (and its
            // antiforgery token) render for a genuinely anonymous request instead, so
            // it stays consistent with the anonymous POST that follows it.
            if (Request.Cookies.ContainsKey("AuthToken"))
            {
                Response.Cookies.Delete("AuthToken");
                return RedirectToAction(nameof(Login), new { returnUrl, stockCompanyId });
            }

            var isAllowed = _connectService.IsAllowedReturnUrl(returnUrl);
            var model = new LoginViewModel
            {
                ReturnUrl = isAllowed ? returnUrl : null,
                StockCompanyId = isAllowed ? stockCompanyId : null
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var company = await _db.Companies.FirstOrDefaultAsync(c => c.Email == model.Email);

            var passwordOk = company is not null &&
                _passwordHasher.VerifyHashedPassword(company, company.PasswordHash, model.Password)
                    != PasswordVerificationResult.Failed;

            if (!passwordOk)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            var token = _tokenService.GenerateToken(company!);

            Response.Cookies.Append("AuthToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Expires = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null,
            });

            // "Connect" flow: hand the Ebill company id + a fresh refresh token back
            // to whichever app sent the user here, instead of going to the invoice screen.
            if (_connectService.IsAllowedReturnUrl(model.ReturnUrl))
            {
                var redirectUrl = await _connectService.IssueTokenAndBuildRedirectUrlAsync(company!, model.ReturnUrl!, model.StockCompanyId);
                return Redirect(redirectUrl);
            }

            return RedirectToAction("Create", "Invoice");
        }

        public IActionResult Logout()
        {
            Response.Cookies.Delete("AuthToken");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var company = await _db.Companies.FirstOrDefaultAsync(c => c.Email == model.Email);

            // Always redirect to the same confirmation regardless of whether the
            // email matched a company, so this form can't be used to discover which
            // addresses are registered.
            if (company is not null)
            {
                var (plainText, hash) = PasswordResetTokenGenerator.Generate();
                company.PasswordResetTokenHash = hash;
                company.PasswordResetTokenExpiresAt = DateTime.UtcNow.Add(PasswordResetTokenGenerator.TokenLifetime);
                await _db.SaveChangesAsync();

                var resetUrl = Url.Action(nameof(ResetPassword), "Account",
                    new { email = company.Email, token = plainText }, protocol: Request.Scheme)!;

                var body = $"<p>Hello {System.Net.WebUtility.HtmlEncode(company.CompanyName)},</p>" +
                    "<p>We received a request to reset your EBill password. Click the link below to choose a new one:</p>" +
                    $"<p><a href=\"{resetUrl}\">Reset your password</a></p>" +
                    "<p>This link expires in 1 hour. If you didn't request this, you can safely ignore this email.</p>";

                try
                {
                    await _emailSender.SendAsync(company.Email, "Reset your EBill password", body);
                }
                catch
                {
                    // Swallowed for the same reason the confirmation message below
                    // doesn't vary — surfacing a mail-send failure here would leak
                    // whether the address exists. If reset emails aren't arriving,
                    // check the Smtp:* settings in appsettings.json.
                }
            }

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string? email = null, string? token = null)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            return View(new ResetPasswordViewModel { Email = email, Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var company = await _db.Companies.FirstOrDefaultAsync(c => c.Email == model.Email);

            var tokenOk = company?.PasswordResetTokenHash is not null &&
                company.PasswordResetTokenExpiresAt is not null &&
                company.PasswordResetTokenExpiresAt > DateTime.UtcNow &&
                company.PasswordResetTokenHash == PasswordResetTokenGenerator.Hash(model.Token);

            if (!tokenOk)
            {
                ModelState.AddModelError(string.Empty,
                    "This password reset link is invalid or has expired. Please request a new one.");
                return View(model);
            }

            company!.PasswordHash = _passwordHasher.HashPassword(company, model.Password);
            company.PasswordResetTokenHash = null;
            company.PasswordResetTokenExpiresAt = null;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
    }
}
