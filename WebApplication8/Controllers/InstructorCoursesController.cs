using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using WebApplication8.Data;
using WebApplication8.Models;
using System.Collections.Generic;

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
        public async Task<IActionResult> Index(string searchString, int pageNumber = 1, int pageSize = 10)
        {
            var instructorCoursesQuery = _context.InstructorCourses
                .Include(ic => ic.Course)
                .Include(ic => ic.User)
                .AsQueryable();

            // Search by course name or user email
            if (!string.IsNullOrEmpty(searchString))
            {
                instructorCoursesQuery = instructorCoursesQuery.Where(ic =>
                    ic.Course.Name.Contains(searchString) ||
                    ic.User.Email.Contains(searchString));
            }

            int totalItems = await instructorCoursesQuery.CountAsync();
            var instructorCourses = await instructorCoursesQuery
                .OrderBy(ic => ic.Course.Name)
                .ThenBy(ic => ic.User.Email)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["CurrentSearch"] = searchString;
            ViewData["PageNumber"] = pageNumber;
            ViewData["PageSize"] = pageSize;
            ViewData["TotalPages"] = (int)System.Math.Ceiling(totalItems / (double)pageSize);

            return View(instructorCourses);
        }

        // GET: InstructorCourses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var instructorCourse = await _context.InstructorCourses
                .Include(ic => ic.Course)
                .Include(ic => ic.User)
                .FirstOrDefaultAsync(ic => ic.Id == id);

            if (instructorCourse == null) return NotFound();

            return View(instructorCourse);
        }

        // GET: InstructorCourses/Create
        public async Task<IActionResult> Create()
        {
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name");

            var instructors = await GetInstructorsAsync();
            ViewData["UserId"] = new SelectList(instructors, "Id", "Email");

            return View();
        }

        // POST: InstructorCourses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UserId,CourseId")] InstructorCourse instructorCourse)
        {
            // ✅ Check if this instructor is already assigned to this course
            bool exists = await _context.InstructorCourses
                .AnyAsync(ic => ic.UserId == instructorCourse.UserId && ic.CourseId == instructorCourse.CourseId);

            if (exists)
            {
                ModelState.AddModelError("", "This instructor is already assigned to this course.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(instructorCourse);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", instructorCourse.CourseId);
            var instructors = await GetInstructorsAsync();
            ViewData["UserId"] = new SelectList(instructors, "Id", "Email", instructorCourse.UserId);

            return View(instructorCourse);
        }

        // GET: InstructorCourses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var instructorCourse = await _context.InstructorCourses.FindAsync(id);
            if (instructorCourse == null) return NotFound();

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", instructorCourse.CourseId);

            var instructors = await GetInstructorsAsync();
            ViewData["UserId"] = new SelectList(instructors, "Id", "Email", instructorCourse.UserId);

            return View(instructorCourse);
        }

        // POST: InstructorCourses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,CourseId")] InstructorCourse instructorCourse)
        {
            if (id != instructorCourse.Id) return NotFound();

            // ✅ Check if another record has the same instructor-course combination
            bool exists = await _context.InstructorCourses
                .AnyAsync(ic => ic.Id != instructorCourse.Id
                             && ic.UserId == instructorCourse.UserId
                             && ic.CourseId == instructorCourse.CourseId);

            if (exists)
            {
                ModelState.AddModelError("", "This instructor is already assigned to this course.");
            }

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
            var instructors = await GetInstructorsAsync();
            ViewData["UserId"] = new SelectList(instructors, "Id", "Email", instructorCourse.UserId);

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

        // Helper to get all users with Instructor role
        private async Task<List<IdentityUser>> GetInstructorsAsync()
        {
            var instructorRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Instructor");
            if (instructorRole == null) return new List<IdentityUser>();

            var instructors = await _context.UserRoles
                .Where(ur => ur.RoleId == instructorRole.Id)
                .Join(_context.Users,
                      ur => ur.UserId,
                      u => u.Id,
                      (ur, u) => u)
                .ToListAsync();

            return instructors;
        }
    }
}
