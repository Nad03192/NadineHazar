using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            if (ModelState.IsValid)
            {
                // ✅ Check if email exists
                var existingEmail = await _userManager.FindByEmailAsync(model.Email);
                if (existingEmail != null)
                {
                    ModelState.AddModelError("Email", "This email is already taken.");
                    return View(model);
                }

                // ✅ Check if username exists
                var existingUserName = await _userManager.FindByNameAsync(model.UserName);
                if (existingUserName != null)
                {
                    ModelState.AddModelError("UserName", "This username is already taken.");
                    return View(model);
                }

                // ✅ Check if phone number exists
                var existingPhone = _userManager.Users.FirstOrDefault(u => u.PhoneNumber == model.PhoneNumber);
                if (existingPhone != null)
                {
                    ModelState.AddModelError("PhoneNumber", "This phone number is already registered.");
                    return View(model);
                }

                // ✅ Create new student user
                var user = new IdentityUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Assign Student role
                    await _userManager.AddToRoleAsync(user, "Student");
                    return RedirectToAction(nameof(Students)); // go back to list
                }

                // Add identity errors if any
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

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

            // Check unique username
            if (await _userManager.FindByNameAsync(model.UserName) != null)
            {
                ModelState.AddModelError("UserName", "Username is already taken.");
                return View(model);
            }

            // Check unique email
            if (!string.IsNullOrEmpty(model.Email))
            {
                if (await _userManager.FindByEmailAsync(model.Email) != null)
                {
                    ModelState.AddModelError("Email", "Email is already taken.");
                    return View(model);
                }
            }

            // Check unique phone
            if (!string.IsNullOrEmpty(model.PhoneNumber))
            {
                var existingUser = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber);
                if (existingUser != null)
                {
                    ModelState.AddModelError("PhoneNumber", "Phone number is already taken.");
                    return View(model);
                }
            }

            var user = new IdentityUser
            {
                UserName = model.UserName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(model);
            }

            // Add to Instructor role
            await _userManager.AddToRoleAsync(user, "Instructor");

            // Send confirmation email
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action("ConfirmEmail", "Account",
                new { userId = user.Id, token },
                protocol: HttpContext.Request.Scheme);

            await _emailSender.SendEmailAsync(model.Email, "Confirm your email",
                $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");

            return RedirectToAction(nameof(Instructors));
        }





        // For Instructors
        public async Task<IActionResult> Instructors(string searchString)
        {
            var users = await _userManager.GetUsersInRoleAsync("Instructor");

            var query = users.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                query = query.Where(u =>
                    u.Email.ToLower().Contains(searchString) ||
                    u.UserName.ToLower().Contains(searchString) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(searchString))
                ).AsQueryable();
            }

            var model = query.Select(u => new UserViewModel
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber
            }).ToList();

            return View(model);
        }


        public async Task<IActionResult> Students(string search)
        {
            var users = await _userManager.GetUsersInRoleAsync("Student"); // Only students

            var query = users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u =>
                    (u.Email != null && u.Email.Contains(search)) ||
                    (u.UserName != null && u.UserName.Contains(search)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));
            }

            ViewBag.Search = search;

            return View(query.ToList());
        }



        [HttpGet]
        public async Task<IActionResult> EditInstructor(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var model = new EditInstructorStudentViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
                // Password fields left empty
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditInstructor(EditInstructorStudentViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            // Check unique username/email/phone (same as before)
            var userByName = await _userManager.FindByNameAsync(model.UserName);
            if (userByName != null && userByName.Id != user.Id)
            {
                ModelState.AddModelError("UserName", "Username is already taken.");
                return View(model);
            }

            var userByEmail = await _userManager.FindByEmailAsync(model.Email);
            if (userByEmail != null && userByEmail.Id != user.Id)
            {
                ModelState.AddModelError("Email", "Email is already taken.");
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.PhoneNumber))
            {
                var existingUser = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber && u.Id != user.Id);
                if (existingUser != null)
                {
                    ModelState.AddModelError("PhoneNumber", "Phone number is already taken.");
                    return View(model);
                }
            }

            // Update main fields
            user.UserName = model.UserName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(model);
            }

            // Update password only if provided
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passResult = await _userManager.ResetPasswordAsync(user, token, model.Password);

                if (!passResult.Succeeded)
                {
                    foreach (var error in passResult.Errors)
                        ModelState.AddModelError("", error.Description);

                    return View(model);
                }
            }

            return RedirectToAction(nameof(Instructors));
        }



        [HttpGet]
        public async Task<IActionResult> DeleteInstructor(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var model = new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }

        [HttpPost, ActionName("DeleteInstructor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteInstructorConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Instructors)); // back to list
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            var model = new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }
        // ================== EDIT STUDENT ==================
        [HttpGet]
        public async Task<IActionResult> EditStudent(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var model = new EditInstructorStudentViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudent(EditInstructorStudentViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            // Unique checks
            var userByName = await _userManager.FindByNameAsync(model.UserName);
            if (userByName != null && userByName.Id != user.Id)
            {
                ModelState.AddModelError("UserName", "Username is already taken.");
                return View(model);
            }

            var userByEmail = await _userManager.FindByEmailAsync(model.Email);
            if (userByEmail != null && userByEmail.Id != user.Id)
            {
                ModelState.AddModelError("Email", "Email is already taken.");
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.PhoneNumber))
            {
                var existingUser = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber && u.Id != user.Id);
                if (existingUser != null)
                {
                    ModelState.AddModelError("PhoneNumber", "Phone number is already taken.");
                    return View(model);
                }
            }

            // Update
            user.UserName = model.UserName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(model);
            }

            // Password change
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passResult = await _userManager.ResetPasswordAsync(user, token, model.Password);

                if (!passResult.Succeeded)
                {
                    foreach (var error in passResult.Errors)
                        ModelState.AddModelError("", error.Description);

                    return View(model);
                }
            }

            return RedirectToAction(nameof(Students));
        }


        // ================== DELETE STUDENT ==================
        [HttpGet]
        public async Task<IActionResult> DeleteStudent(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var model = new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }

        [HttpPost, ActionName("DeleteStudent")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStudentConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Students));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            var model = new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }

    }
}