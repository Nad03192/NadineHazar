using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication8.Data;
using WebApplication8.Models;

namespace WebApplication8.Controllers
{
    public class ProgramCoursesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ProgramCoursesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Helper method to get the logged-in user's managed study program
        private async Task<int?> GetManagedStudyProgramId()
        {
            var userId = _userManager.GetUserId(User);
            if (User.IsInRole("Admin"))
                return null; // Admin can access everything

            var manager = await _context.ProgramManager
                .FirstOrDefaultAsync(pm => pm.UserId == userId);

            return manager?.StudyProgramId;
        }

        // GET: ProgramCourses
        public async Task<IActionResult> Index()
        {
            var managedProgramId = await GetManagedStudyProgramId();

            var query = _context.ProgramCourses
                .Include(p => p.Course)
                .Include(p => p.CourseType)
                .Include(p => p.StudyProgram)
                .AsQueryable();

            if (managedProgramId.HasValue)
            {
                query = query.Where(pc => pc.StudyProgramId == managedProgramId.Value);
            }

            return View(await query.ToListAsync());
        }

        // GET: ProgramCourses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var programCourse = await _context.ProgramCourses
                .Include(p => p.Course)
                .Include(p => p.CourseType)
                .Include(p => p.StudyProgram)
                .FirstOrDefaultAsync(m => m.StudyProgramId == id);

            if (programCourse == null) return NotFound();

            var managedProgramId = await GetManagedStudyProgramId();
            if (managedProgramId.HasValue && programCourse.StudyProgramId != managedProgramId.Value)
                return Forbid();

            return View(programCourse);
        }

        // GET: ProgramCourses/Create
        public async Task<IActionResult> Create()
        {
            var managedProgramId = await GetManagedStudyProgramId();

            if (managedProgramId.HasValue)
            {
                ViewData["StudyProgramId"] = new SelectList(
                    _context.StudyPrograms.Where(sp => sp.StudyProgramId == managedProgramId.Value),
                    "StudyProgramId", "Name");
            }
            else
            {
                ViewData["StudyProgramId"] = new SelectList(_context.StudyPrograms, "StudyProgramId", "Name");
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseId");
            ViewData["CourseTypeId"] = new SelectList(_context.CourseTypes, "CourseTypeId", "Name");
            return View();
        }

        // POST: ProgramCourses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StudyProgramId,CourseId,CourseTypeId")] ProgramCourse programCourse)
        {
            var managedProgramId = await GetManagedStudyProgramId();
            if (managedProgramId.HasValue && programCourse.StudyProgramId != managedProgramId.Value)
                return Forbid();

            if (ModelState.IsValid)
            {
                _context.Add(programCourse);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseId", programCourse.CourseId);
            ViewData["CourseTypeId"] = new SelectList(_context.CourseTypes, "CourseTypeId", "Name", programCourse.CourseTypeId);
            ViewData["StudyProgramId"] = new SelectList(_context.StudyPrograms, "StudyProgramId", "Name", programCourse.StudyProgramId);
            return View(programCourse);
        }

        // GET: ProgramCourses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var programCourse = await _context.ProgramCourses.FindAsync(id);
            if (programCourse == null) return NotFound();

            var managedProgramId = await GetManagedStudyProgramId();
            if (managedProgramId.HasValue && programCourse.StudyProgramId != managedProgramId.Value)
                return Forbid();

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseId", programCourse.CourseId);
            ViewData["CourseTypeId"] = new SelectList(_context.CourseTypes, "CourseTypeId", "Name", programCourse.CourseTypeId);
            ViewData["StudyProgramId"] = new SelectList(_context.StudyPrograms, "StudyProgramId", "Name", programCourse.StudyProgramId);
            return View(programCourse);
        }

        // POST: ProgramCourses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StudyProgramId,CourseId,CourseTypeId")] ProgramCourse programCourse)
        {
            if (id != programCourse.StudyProgramId) return NotFound();

            var managedProgramId = await GetManagedStudyProgramId();
            if (managedProgramId.HasValue && programCourse.StudyProgramId != managedProgramId.Value)
                return Forbid();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(programCourse);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProgramCourseExists(programCourse.StudyProgramId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseId", programCourse.CourseId);
            ViewData["CourseTypeId"] = new SelectList(_context.CourseTypes, "CourseTypeId", "Name", programCourse.CourseTypeId);
            ViewData["StudyProgramId"] = new SelectList(_context.StudyPrograms, "StudyProgramId", "Name", programCourse.StudyProgramId);
            return View(programCourse);
        }

        // GET: ProgramCourses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var programCourse = await _context.ProgramCourses
                .Include(p => p.Course)
                .Include(p => p.CourseType)
                .Include(p => p.StudyProgram)
                .FirstOrDefaultAsync(m => m.StudyProgramId == id);

            if (programCourse == null) return NotFound();

            var managedProgramId = await GetManagedStudyProgramId();
            if (managedProgramId.HasValue && programCourse.StudyProgramId != managedProgramId.Value)
                return Forbid();

            return View(programCourse);
        }

        // POST: ProgramCourses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var programCourse = await _context.ProgramCourses.FindAsync(id);

            var managedProgramId = await GetManagedStudyProgramId();
            if (managedProgramId.HasValue && programCourse.StudyProgramId != managedProgramId.Value)
                return Forbid();

            if (programCourse != null)
            {
                _context.ProgramCourses.Remove(programCourse);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProgramCourseExists(int id)
        {
            return _context.ProgramCourses.Any(e => e.StudyProgramId == id);
        }
    }
}
