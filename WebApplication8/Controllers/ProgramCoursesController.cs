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
    public class ProgramCoursesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProgramCoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ProgramCourses
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.ProgramCourses.Include(p => p.Course).Include(p => p.CourseType).Include(p => p.StudyProgram);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: ProgramCourses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var programCourse = await _context.ProgramCourses
                .Include(p => p.Course)
                .Include(p => p.CourseType)
                .Include(p => p.StudyProgram)
                .FirstOrDefaultAsync(m => m.StudyProgramId == id);
            if (programCourse == null)
            {
                return NotFound();
            }

            return View(programCourse);
        }

        // GET: ProgramCourses/Create
        public IActionResult Create()
        {
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseId");
            ViewData["CourseTypeId"] = new SelectList(_context.CourseTypes, "CourseTypeId", "Name");
            ViewData["StudyProgramId"] = new SelectList(_context.StudyPrograms, "StudyProgramId", "Name");
            return View();
        }

        // POST: ProgramCourses/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StudyProgramId,CourseId,CourseTypeId")] ProgramCourse programCourse)
        {
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
            if (id == null)
            {
                return NotFound();
            }

            var programCourse = await _context.ProgramCourses.FindAsync(id);
            if (programCourse == null)
            {
                return NotFound();
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseId", programCourse.CourseId);
            ViewData["CourseTypeId"] = new SelectList(_context.CourseTypes, "CourseTypeId", "Name", programCourse.CourseTypeId);
            ViewData["StudyProgramId"] = new SelectList(_context.StudyPrograms, "StudyProgramId", "Name", programCourse.StudyProgramId);
            return View(programCourse);
        }

        // POST: ProgramCourses/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StudyProgramId,CourseId,CourseTypeId")] ProgramCourse programCourse)
        {
            if (id != programCourse.StudyProgramId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(programCourse);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProgramCourseExists(programCourse.StudyProgramId))
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
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseId", programCourse.CourseId);
            ViewData["CourseTypeId"] = new SelectList(_context.CourseTypes, "CourseTypeId", "Name", programCourse.CourseTypeId);
            ViewData["StudyProgramId"] = new SelectList(_context.StudyPrograms, "StudyProgramId", "Name", programCourse.StudyProgramId);
            return View(programCourse);
        }

        // GET: ProgramCourses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var programCourse = await _context.ProgramCourses
                .Include(p => p.Course)
                .Include(p => p.CourseType)
                .Include(p => p.StudyProgram)
                .FirstOrDefaultAsync(m => m.StudyProgramId == id);
            if (programCourse == null)
            {
                return NotFound();
            }

            return View(programCourse);
        }

        // POST: ProgramCourses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var programCourse = await _context.ProgramCourses.FindAsync(id);
            if (programCourse != null)
            {
                _context.ProgramCourses.Remove(programCourse);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProgramCourseExists(int id)
        {
            return _context.ProgramCourses.Any(e => e.StudyProgramId == id);
        }
    }
}
