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
    public class StudentGradesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudentGradesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: StudentGrades
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.StudentGrades.Include(s => s.Enrollment).Include(s => s.GradeDefinition);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: StudentGrades/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var studentGrade = await _context.StudentGrades
                .Include(s => s.Enrollment)
                .Include(s => s.GradeDefinition)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (studentGrade == null)
            {
                return NotFound();
            }

            return View(studentGrade);
        }

        // GET: StudentGrades/Create
        public IActionResult Create()
        {
            ViewData["EnrollmentId"] = new SelectList(_context.Enrollments, "Id", "Id");
            ViewData["GradeDefinitionId"] = new SelectList(_context.GradeDefinitions, "Id", "Name");
            return View();
        }

        // POST: StudentGrades/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,EnrollmentId,GradeDefinitionId,Score")] StudentGrade studentGrade)
        {
            if (ModelState.IsValid)
            {
                _context.Add(studentGrade);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["EnrollmentId"] = new SelectList(_context.Enrollments, "Id", "Id", studentGrade.EnrollmentId);
            ViewData["GradeDefinitionId"] = new SelectList(_context.GradeDefinitions, "Id", "Name", studentGrade.GradeDefinitionId);
            return View(studentGrade);
        }

        // GET: StudentGrades/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var studentGrade = await _context.StudentGrades.FindAsync(id);
            if (studentGrade == null)
            {
                return NotFound();
            }
            ViewData["EnrollmentId"] = new SelectList(_context.Enrollments, "Id", "Id", studentGrade.EnrollmentId);
            ViewData["GradeDefinitionId"] = new SelectList(_context.GradeDefinitions, "Id", "Name", studentGrade.GradeDefinitionId);
            return View(studentGrade);
        }

        // POST: StudentGrades/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,EnrollmentId,GradeDefinitionId,Score")] StudentGrade studentGrade)
        {
            if (id != studentGrade.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(studentGrade);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentGradeExists(studentGrade.Id))
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
            ViewData["EnrollmentId"] = new SelectList(_context.Enrollments, "Id", "Id", studentGrade.EnrollmentId);
            ViewData["GradeDefinitionId"] = new SelectList(_context.GradeDefinitions, "Id", "Name", studentGrade.GradeDefinitionId);
            return View(studentGrade);
        }

        // GET: StudentGrades/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var studentGrade = await _context.StudentGrades
                .Include(s => s.Enrollment)
                .Include(s => s.GradeDefinition)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (studentGrade == null)
            {
                return NotFound();
            }

            return View(studentGrade);
        }

        // POST: StudentGrades/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var studentGrade = await _context.StudentGrades.FindAsync(id);
            if (studentGrade != null)
            {
                _context.StudentGrades.Remove(studentGrade);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StudentGradeExists(int id)
        {
            return _context.StudentGrades.Any(e => e.Id == id);
        }
    }
}
