using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication8.Controllers
{
    [Authorize(Roles = "Instructor")]
    public class InstructorController : Controller
    {
        public IActionResult Home()
        {
            return View(); // Loads Views/Instructor/Home.cshtml
        }
    }
}
