using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication8.Data;
using WebApplication8.Models;

namespace WebApplication8.Controllers
{
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchName, string searchDescription, int? studyProgramId, int? courseTypeId)
        {
            var coursesQuery = _context.Courses
                .Include(c => c.ProgramCourses)
                    .ThenInclude(pc => pc.StudyProgram)
                .Include(c => c.ProgramCourses)
                    .ThenInclude(pc => pc.CourseType)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchName))
                coursesQuery = coursesQuery.Where(c => c.Name.Contains(searchName));

            if (!string.IsNullOrEmpty(searchDescription))
                coursesQuery = coursesQuery.Where(c => c.Description.Contains(searchDescription));

            if (studyProgramId.HasValue)
                coursesQuery = coursesQuery.Where(c => c.ProgramCourses.Any(pc => pc.StudyProgramId == studyProgramId.Value));

            if (courseTypeId.HasValue)
                coursesQuery = coursesQuery.Where(c => c.ProgramCourses.Any(pc => pc.CourseTypeId == courseTypeId.Value));

            var model = await coursesQuery.ToListAsync();

            // Prepare dropdowns for filters
            ViewData["StudyPrograms"] = new SelectList(await _context.StudyPrograms.ToListAsync(), "StudyProgramId", "Name");
            ViewData["CourseTypes"] = new SelectList(await _context.CourseTypes.ToListAsync(), "CourseTypeId", "Name");

            return View(model);
        }



        // GET: Courses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .FirstOrDefaultAsync(m => m.CourseId == id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // GET: Courses/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Courses/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CourseId,Name,Description,CreditNumber")] Course course)
        {
            // Check if a course with the same name already exists
            bool nameExists = await _context.Courses.AnyAsync(c => c.Name == course.Name);
            if (nameExists)
            {
                ModelState.AddModelError("Name", "A course with this name already exists.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(course);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(course);
        }

        // GET: Courses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }
            return View(course);
        }

        // POST: Courses/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CourseId,Name,Description,CreditNumber")] Course course)
        {
            if (id != course.CourseId)
            {
                return NotFound();
            }

            // Check if a course with the same name exists, excluding the current one
            bool nameExists = await _context.Courses
                .AnyAsync(c => c.Name == course.Name && c.CourseId != course.CourseId);

            if (nameExists)
            {
                ModelState.AddModelError("Name", "A course with this name already exists.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(course);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.CourseId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(course);
        }


        // GET: Courses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .FirstOrDefaultAsync(m => m.CourseId == id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // POST: Courses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course != null)
            {
                _context.Courses.Remove(course);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.CourseId == id);
        }
    }
}
