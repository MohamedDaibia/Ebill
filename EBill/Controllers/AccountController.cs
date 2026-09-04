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

        public AccountController(ApplicationDbContext db, IPasswordHasher<Company> passwordHasher, JwtTokenService tokenService)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
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

            return RedirectToAction("Create", "Invoice");
        }

        public IActionResult Logout()
        {
            Response.Cookies.Delete("AuthToken");
            return RedirectToAction("Index", "Home");
        }
    }
}
