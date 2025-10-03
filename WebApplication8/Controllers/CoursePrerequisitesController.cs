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
    public class CoursePrerequisitesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CoursePrerequisitesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: CoursePrerequisites
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.CoursePrerequisites.Include(c => c.Course).Include(c => c.Prerequisite);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: CoursePrerequisites/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coursePrerequisite = await _context.CoursePrerequisites
                .Include(c => c.Course)
                .Include(c => c.Prerequisite)
                .FirstOrDefaultAsync(m => m.CourseId == id);
            if (coursePrerequisite == null)
            {
                return NotFound();
            }

            return View(coursePrerequisite);
        }

        // GET: CoursePrerequisites/Create
        public IActionResult Create()
        {
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name");
            ViewData["PrerequisiteId"] = new SelectList(_context.Courses, "CourseId", "Name");
            return View();
        }

        // POST: CoursePrerequisites/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CourseId,PrerequisiteId")] CoursePrerequisite coursePrerequisite)
        {
            if (coursePrerequisite.CourseId == coursePrerequisite.PrerequisiteId)
            {
                ModelState.AddModelError(string.Empty, "A course cannot be a prerequisite of itself.");
            }

            // Check if the same pair already exists
            bool directExists = await _context.CoursePrerequisites
                .AnyAsync(cp => cp.CourseId == coursePrerequisite.CourseId
                               && cp.PrerequisiteId == coursePrerequisite.PrerequisiteId);

            // Check if the reverse pair exists
            bool reverseExists = await _context.CoursePrerequisites
                .AnyAsync(cp => cp.CourseId == coursePrerequisite.PrerequisiteId
                               && cp.PrerequisiteId == coursePrerequisite.CourseId);

            if (directExists)
            {
                ModelState.AddModelError(string.Empty, "This course prerequisite already exists.");
            }
            else if (reverseExists)
            {
                ModelState.AddModelError(string.Empty, "Cannot add: the reverse prerequisite already exists.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(coursePrerequisite);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", coursePrerequisite.CourseId);
            ViewData["PrerequisiteId"] = new SelectList(_context.Courses, "CourseId", "Name", coursePrerequisite.PrerequisiteId);
            return View(coursePrerequisite);
        }


        // GET: CoursePrerequisites/Edit/5
        // GET: CoursePrerequisites/Edit/5/3
        public async Task<IActionResult> Edit(int courseId, int prerequisiteId)
        {
            var coursePrerequisite = await _context.CoursePrerequisites
                .FindAsync(courseId, prerequisiteId);

            if (coursePrerequisite == null)
                return NotFound();

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", coursePrerequisite.CourseId);
            ViewData["PrerequisiteId"] = new SelectList(_context.Courses, "CourseId", "Name", coursePrerequisite.PrerequisiteId);
            return View(coursePrerequisite);
        }


        // POST: CoursePrerequisites/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int courseId, int prerequisiteId, [Bind("CourseId,PrerequisiteId")] CoursePrerequisite coursePrerequisite)
        {
            if (courseId != coursePrerequisite.CourseId || prerequisiteId != coursePrerequisite.PrerequisiteId)
                return BadRequest();

            // Optional: add the duplicate/reverse check as in Create action
            bool duplicateExists = await _context.CoursePrerequisites
                .AnyAsync(cp => cp.CourseId == coursePrerequisite.CourseId && cp.PrerequisiteId == coursePrerequisite.PrerequisiteId
                             && !(cp.CourseId == courseId && cp.PrerequisiteId == prerequisiteId));

            bool reverseExists = await _context.CoursePrerequisites
                .AnyAsync(cp => cp.CourseId == coursePrerequisite.PrerequisiteId && cp.PrerequisiteId == coursePrerequisite.CourseId);

            if (duplicateExists)
            {
                ModelState.AddModelError(string.Empty, "This prerequisite already exists.");
            }
            else if (reverseExists)
            {
                ModelState.AddModelError(string.Empty, "Cannot add: the reverse prerequisite already exists.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(coursePrerequisite);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CoursePrerequisiteExists(coursePrerequisite.CourseId, coursePrerequisite.PrerequisiteId))
                        return NotFound();
                    else
                        throw;
                }
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", coursePrerequisite.CourseId);
            ViewData["PrerequisiteId"] = new SelectList(_context.Courses, "CourseId", "Name", coursePrerequisite.PrerequisiteId);
            return View(coursePrerequisite);
        }

        // GET: CoursePrerequisites/Delete/5
        // GET: CoursePrerequisites/Delete/5/3
        public async Task<IActionResult> Delete(int courseId, int prerequisiteId)
        {
            var coursePrerequisite = await _context.CoursePrerequisites
                .Include(c => c.Course)
                .Include(c => c.Prerequisite)
                .FirstOrDefaultAsync(cp => cp.CourseId == courseId && cp.PrerequisiteId == prerequisiteId);

            if (coursePrerequisite == null)
                return NotFound();

            return View(coursePrerequisite);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int courseId, int prerequisiteId)
        {
            var coursePrerequisite = await _context.CoursePrerequisites.FindAsync(courseId, prerequisiteId);
            if (coursePrerequisite != null)
            {
                _context.CoursePrerequisites.Remove(coursePrerequisite);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }


        private bool CoursePrerequisiteExists(int courseId, int prerequisiteId)
        {
            return _context.CoursePrerequisites
                .Any(e => e.CourseId == courseId && e.PrerequisiteId == prerequisiteId);
        }

    }
}
