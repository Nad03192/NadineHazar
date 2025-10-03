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
    public class GradeDefinitionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GradeDefinitionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: GradeDefinitions
        public async Task<IActionResult> Index(string searchCourse, int? studyProgramId)
        {
            // Base query including Course and ProgramCourses for filtering
            var gradeDefinitions = _context.GradeDefinitions
                .Include(g => g.Course)
                .ThenInclude(c => c.ProgramCourses)
                .ThenInclude(pc => pc.StudyProgram)
                .AsQueryable();

            // Filter by course name
            if (!string.IsNullOrEmpty(searchCourse))
                gradeDefinitions = gradeDefinitions.Where(g => g.Course.Name.Contains(searchCourse));

            // Filter by study program
            if (studyProgramId.HasValue)
                gradeDefinitions = gradeDefinitions.Where(g =>
                    g.Course.ProgramCourses.Any(pc => pc.StudyProgramId == studyProgramId.Value));

            // Pass filter values to ViewBag for the form
            ViewBag.SearchCourse = searchCourse;
            ViewBag.StudyPrograms = new SelectList(_context.StudyPrograms, "StudyProgramId", "Name", studyProgramId);

            return View(await gradeDefinitions.ToListAsync());
        }

        // GET: GradeDefinitions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gradeDefinition = await _context.GradeDefinitions
                .Include(g => g.Course)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (gradeDefinition == null)
            {
                return NotFound();
            }

            return View(gradeDefinition);
        }

        // GET: GradeDefinitions/Create
        public IActionResult Create()
        {
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name");
            return View();
        }

        // POST: GradeDefinitions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CourseId,Name,Coefficient")] GradeDefinition gradeDefinition)
        {
            // Check total coefficient for this course
            var totalCoefficient = await _context.GradeDefinitions
                .Where(g => g.CourseId == gradeDefinition.CourseId)
                .SumAsync(g => g.Coefficient);

            if (totalCoefficient + gradeDefinition.Coefficient > 1.0)
            {
                ModelState.AddModelError("Coefficient",
                    $"Total coefficient for this course cannot exceed 1. " +
                    $"Currently at {totalCoefficient}, adding {gradeDefinition.Coefficient} would exceed the limit.");
            }

            // ✅ Check if name already exists for this course
            bool nameExists = await _context.GradeDefinitions
                .AnyAsync(g => g.CourseId == gradeDefinition.CourseId && g.Name == gradeDefinition.Name);

            if (nameExists)
            {
                ModelState.AddModelError("Name", $"A grade definition with this name already exists for this course.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(gradeDefinition);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", gradeDefinition.CourseId);
            return View(gradeDefinition);
        }


        // GET: GradeDefinitions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gradeDefinition = await _context.GradeDefinitions.FindAsync(id);
            if (gradeDefinition == null)
            {
                return NotFound();
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", gradeDefinition.CourseId);
            return View(gradeDefinition);
        }

        // POST: GradeDefinitions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CourseId,Name,Coefficient")] GradeDefinition gradeDefinition)
        {
            if (id != gradeDefinition.Id)
            {
                return NotFound();
            }

            // Check total coefficient excluding current record
            var totalCoefficient = await _context.GradeDefinitions
                .Where(g => g.CourseId == gradeDefinition.CourseId && g.Id != gradeDefinition.Id)
                .SumAsync(g => g.Coefficient);

            if (totalCoefficient + gradeDefinition.Coefficient > 1.0)
            {
                ModelState.AddModelError("Coefficient",
                    $"Total coefficient for this course cannot exceed 1. " +
                    $"Currently at {totalCoefficient}, updating to {gradeDefinition.Coefficient} would exceed the limit.");
            }

            // ✅ Check if name already exists for this course excluding current record
            bool nameExists = await _context.GradeDefinitions
                .Where(g => g.Id != gradeDefinition.Id)
                .AnyAsync(g => g.CourseId == gradeDefinition.CourseId && g.Name == gradeDefinition.Name);

            if (nameExists)
            {
                ModelState.AddModelError("Name", $"A grade definition with this name already exists for this course.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(gradeDefinition);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GradeDefinitionExists(gradeDefinition.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", gradeDefinition.CourseId);
            return View(gradeDefinition);
        }


        // GET: GradeDefinitions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gradeDefinition = await _context.GradeDefinitions
                .Include(g => g.Course)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (gradeDefinition == null)
            {
                return NotFound();
            }

            return View(gradeDefinition);
        }

        // POST: GradeDefinitions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var gradeDefinition = await _context.GradeDefinitions.FindAsync(id);
            if (gradeDefinition != null)
            {
                _context.GradeDefinitions.Remove(gradeDefinition);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GradeDefinitionExists(int id)
        {
            return _context.GradeDefinitions.Any(e => e.Id == id);
        }
    }
}
