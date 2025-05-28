using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using WebApplication8.Data;
using WebApplication8.Models;

namespace WebApplication8.Controllers
{
    public class InstructorCoursesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public InstructorCoursesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: InstructorCourses
        public async Task<IActionResult> Index()
        {
            var instructorCourses = _context.InstructorCourses
                .Include(ic => ic.Course)
                .Include(ic => ic.User);
            return View(await instructorCourses.ToListAsync());
        }

        // GET: InstructorCourses/Create
        public async Task<IActionResult> Create()
        {
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name");

            // Get the "Instructor" role
            var instructorRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Instructor");
            if (instructorRole == null)
            {
                // Handle role not found
                ViewData["UserId"] = new SelectList(Enumerable.Empty<IdentityUser>(), "Id", "UserName");
                return View();
            }

            // Get users who have the "Instructor" role
            var instructors = await _context.UserRoles
                .Where(ur => ur.RoleId == instructorRole.Id)
                .Join(_context.Users,
                      ur => ur.UserId,
                      u => u.Id,
                      (ur, u) => u)
                .ToListAsync();

            ViewData["UserId"] = new SelectList(instructors, "Id", "UserName");

            return View();
        }

        // POST: InstructorCourses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UserId,CourseId")] InstructorCourse instructorCourse)
        {
            if (ModelState.IsValid)
            {
                _context.Add(instructorCourse);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", instructorCourse.CourseId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", instructorCourse.UserId);

            return View(instructorCourse);
        }

        // GET: InstructorCourses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var instructorCourse = await _context.InstructorCourses.FindAsync(id);
            if (instructorCourse == null) return NotFound();

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", instructorCourse.CourseId);

            var instructorRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Instructor");
            if (instructorRole == null)
            {
                ViewData["UserId"] = new SelectList(Enumerable.Empty<IdentityUser>(), "Id", "UserName");
                return View(instructorCourse);
            }

            var instructors = await _context.UserRoles
                .Where(ur => ur.RoleId == instructorRole.Id)
                .Join(_context.Users,
                      ur => ur.UserId,
                      u => u.Id,
                      (ur, u) => u)
                .ToListAsync();

            ViewData["UserId"] = new SelectList(instructors, "Id", "UserName", instructorCourse.UserId);

            return View(instructorCourse);
        }

        // POST: InstructorCourses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,CourseId")] InstructorCourse instructorCourse)
        {
            if (id != instructorCourse.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(instructorCourse);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InstructorCourseExists(instructorCourse.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", instructorCourse.CourseId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", instructorCourse.UserId);

            return View(instructorCourse);
        }

        // GET: InstructorCourses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var instructorCourse = await _context.InstructorCourses
                .Include(ic => ic.Course)
                .Include(ic => ic.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (instructorCourse == null) return NotFound();

            return View(instructorCourse);
        }

        // POST: InstructorCourses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var instructorCourse = await _context.InstructorCourses.FindAsync(id);
            if (instructorCourse != null)
            {
                _context.InstructorCourses.Remove(instructorCourse);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool InstructorCourseExists(int id)
        {
            return _context.InstructorCourses.Any(e => e.Id == id);
        }
        // GET: InstructorCourses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var instructorCourse = await _context.InstructorCourses
                .Include(ic => ic.Course)     // Include related Course
                .Include(ic => ic.User)       // Include related IdentityUser
                .FirstOrDefaultAsync(ic => ic.Id == id);

            if (instructorCourse == null)
                return NotFound();

            return View(instructorCourse);
        }

    }

}
