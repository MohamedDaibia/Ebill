using EBill.Models;
using Microsoft.AspNetCore.Mvc;

namespace EBill.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Login is not available yet — authentication has not been set up.");
            return View(model);
        }
    }
}
