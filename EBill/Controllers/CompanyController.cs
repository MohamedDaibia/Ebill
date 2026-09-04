using EBill.Data;
using EBill.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EBill.Controllers
{
    public class CompanyController : Controller
    {
        private static readonly string[] AllowedLogoExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".svg" };
        private const long MaxLogoSizeBytes = 2 * 1024 * 1024; // 2 MB

        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _db;
        private readonly IPasswordHasher<Company> _passwordHasher;

        public CompanyController(IWebHostEnvironment env, ApplicationDbContext db, IPasswordHasher<Company> passwordHasher)
        {
            _env = env;
            _db = db;
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new Company());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Company model)
        {
            if (model.LogoFile is { Length: > 0 } logo)
            {
                var extension = Path.GetExtension(logo.FileName).ToLowerInvariant();

                if (!AllowedLogoExtensions.Contains(extension))
                {
                    ModelState.AddModelError(nameof(model.LogoFile), "Only JPG, PNG, GIF, or SVG images are allowed.");
                }
                else if (logo.Length > MaxLogoSizeBytes)
                {
                    ModelState.AddModelError(nameof(model.LogoFile), "Logo must be 2 MB or smaller.");
                }
            }

            if (await _db.Companies.AnyAsync(c => c.Email == model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "A company is already registered with this email address.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.LogoFile is { Length: > 0 } validLogo)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "logos");
                Directory.CreateDirectory(uploadsFolder);

                var extension = Path.GetExtension(validLogo.FileName).ToLowerInvariant();
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await validLogo.CopyToAsync(stream);
                }

                model.LogoPath = $"/uploads/logos/{fileName}";
            }

            model.PasswordHash = _passwordHasher.HashPassword(model, model.Password);

            _db.Companies.Add(model);
            await _db.SaveChangesAsync();

            return View("Confirmation", model);
        }
    }
}
