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
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(
     UserManager<IdentityUser> userManager,
     IEmailSender emailSender,
     RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _roleManager = roleManager;
        }

        public IActionResult Home()
        {
            return View();
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

            // Use UserName from model instead of Email
            var user = new IdentityUser
            {
                UserName = model.UserName,    // 👈 new
                Email = model.Email,
                PhoneNumber = model.PhoneNumber  // 👈 new
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Student");

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

            var user = new IdentityUser
            {
                UserName = model.UserName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Instructor");

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action("ConfirmEmail", "Account",
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




        // For Instructors
        public async Task<IActionResult> Instructors()
        {
            var users = _userManager.Users.ToList();
            var list = new List<UserViewModel>();

            foreach (var user in users)
            {
                if (await _userManager.IsInRoleAsync(user, "Instructor"))
                {
                    list.Add(new UserViewModel
                    {
                        Id = user.Id,
                        Email = user.Email,
                        UserName = user.UserName,
                        PhoneNumber = user.PhoneNumber,
                        Role = "Instructor"
                    });
                }
            }

            return View(list);
        }

        public async Task<IActionResult> Students()
        {
            var users = _userManager.Users.ToList();
            var students = new List<IdentityUser>();

            foreach (var user in users)
            {
                if (await _userManager.IsInRoleAsync(user, "Student"))
                    students.Add(user);
            }

            return View(students);
        }

    }
}