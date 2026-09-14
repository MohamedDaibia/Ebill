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

        public AccountController(
            ApplicationDbContext db,
            IPasswordHasher<Company> passwordHasher,
            JwtTokenService tokenService,
            EbillConnectService connectService)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _connectService = connectService;
        }

        // returnUrl is set when another app (e.g. Stock Management's "Sync Ebill"
        // button) sent the user here to connect — only kept if it's on the
        // AllowedConnectOrigins allowlist, otherwise treated as if absent.
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            var model = new LoginViewModel
            {
                ReturnUrl = _connectService.IsAllowedReturnUrl(returnUrl) ? returnUrl : null
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
                var redirectUrl = await _connectService.IssueTokenAndBuildRedirectUrlAsync(company!, model.ReturnUrl!);
                return Redirect(redirectUrl);
            }

            return RedirectToAction("Create", "Invoice");
        }

        public IActionResult Logout()
        {
            Response.Cookies.Delete("AuthToken");
            return RedirectToAction("Index", "Home");
        }
    }
}
