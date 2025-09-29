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
    public class StudyProgramsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudyProgramsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: StudyPrograms
        public async Task<IActionResult> Index(string searchString)
        {
            ViewBag.CurrentFilter = searchString;

            var studyPrograms = _context.StudyPrograms
                .Include(s => s.Faculty)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                studyPrograms = studyPrograms.Where(s =>
                    s.Name.Contains(searchString) ||
                    s.Faculty.Name.Contains(searchString));
            }

            return View(await studyPrograms.ToListAsync());
        }


        // GET: StudyPrograms/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var studyProgram = await _context.StudyPrograms
                .Include(s => s.Faculty)
                .FirstOrDefaultAsync(m => m.StudyProgramId == id);
            if (studyProgram == null)
            {
                return NotFound();
            }

            return View(studyProgram);
        }

        // GET: StudyPrograms/Create
        public IActionResult Create()
        {
            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "Name");
            return View();
        }

        // POST: StudyPrograms/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StudyProgramId,Name,FacultyId,TotalCredits")] StudyProgram studyProgram)
        {
            if (ModelState.IsValid)
            {
                _context.Add(studyProgram);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "Name", studyProgram.FacultyId);
            return View(studyProgram);
        }


        // GET: StudyPrograms/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var studyProgram = await _context.StudyPrograms.FindAsync(id);
            if (studyProgram == null)
            {
                return NotFound();
            }
            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "Name", studyProgram.FacultyId);
            return View(studyProgram);
        }

        // POST: StudyPrograms/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StudyProgramId,Name,FacultyId,TotalCredits")] StudyProgram studyProgram)
        {
            if (id != studyProgram.StudyProgramId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(studyProgram);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudyProgramExists(studyProgram.StudyProgramId))
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
            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "Name", studyProgram.FacultyId);
            return View(studyProgram);
        }

        // GET: StudyPrograms/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var studyProgram = await _context.StudyPrograms
                .Include(s => s.Faculty)
                .FirstOrDefaultAsync(m => m.StudyProgramId == id);
            if (studyProgram == null)
            {
                return NotFound();
            }

            return View(studyProgram);
        }

        // POST: StudyPrograms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var studyProgram = await _context.StudyPrograms.FindAsync(id);
            if (studyProgram != null)
            {
                _context.StudyPrograms.Remove(studyProgram);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StudyProgramExists(int id)
        {
            return _context.StudyPrograms.Any(e => e.StudyProgramId == id);
        }
    }
}
