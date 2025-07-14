using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebApplication8.Models;  // Adjust namespace for RegisterUserViewModel

namespace WebApplication8.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

        public AdminController(UserManager<IdentityUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        public IActionResult Home()
        {
            return View(); // Views/Admin/Home.cshtml
        }

        // GET: Create Student form
        public IActionResult CreateStudent()
        {
            return View();
        }

        // POST: Create Student
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStudent(RegisterUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new IdentityUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Student");

                // Generate email confirmation token
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                // Generate confirmation URL
                var confirmationLink = Url.Action(
                    "ConfirmEmail", "Account",
                    new { userId = user.Id, token },
                    protocol: HttpContext.Request.Scheme);

                // Send confirmation email
                await _emailSender.SendEmailAsync(model.Email, "Confirm your email",
                    $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");

                return RedirectToAction(nameof(Home));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // GET: Create Instructor form
        public IActionResult CreateInstructor()
        {
            return View();
        }

        // POST: Create Instructor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInstructor(RegisterUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new IdentityUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Instructor");

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                var confirmationLink = Url.Action(
                    "ConfirmEmail", "Account",
                    new { userId = user.Id, token },
                    protocol: HttpContext.Request.Scheme);

                await _emailSender.SendEmailAsync(model.Email, "Confirm your email",
                    $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");

                return RedirectToAction(nameof(Home));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }
    }
}
