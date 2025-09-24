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
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseId");
            ViewData["PrerequisiteId"] = new SelectList(_context.Courses, "CourseId", "CourseId");
            return View();
        }

        // POST: CoursePrerequisites/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CourseId,PrerequisiteId")] CoursePrerequisite coursePrerequisite)
        {
            if (ModelState.IsValid)
            {
                _context.Add(coursePrerequisite);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseId", coursePrerequisite.CourseId);
            ViewData["PrerequisiteId"] = new SelectList(_context.Courses, "CourseId", "CourseId", coursePrerequisite.PrerequisiteId);
            return View(coursePrerequisite);
        }

        // GET: CoursePrerequisites/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coursePrerequisite = await _context.CoursePrerequisites.FindAsync(id);
            if (coursePrerequisite == null)
            {
                return NotFound();
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseId", coursePrerequisite.CourseId);
            ViewData["PrerequisiteId"] = new SelectList(_context.Courses, "CourseId", "CourseId", coursePrerequisite.PrerequisiteId);
            return View(coursePrerequisite);
        }

        // POST: CoursePrerequisites/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CourseId,PrerequisiteId")] CoursePrerequisite coursePrerequisite)
        {
            if (id != coursePrerequisite.CourseId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(coursePrerequisite);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CoursePrerequisiteExists(coursePrerequisite.CourseId))
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
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseId", coursePrerequisite.CourseId);
            ViewData["PrerequisiteId"] = new SelectList(_context.Courses, "CourseId", "CourseId", coursePrerequisite.PrerequisiteId);
            return View(coursePrerequisite);
        }

        // GET: CoursePrerequisites/Delete/5
        public async Task<IActionResult> Delete(int? id)
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

        // POST: CoursePrerequisites/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var coursePrerequisite = await _context.CoursePrerequisites.FindAsync(id);
            if (coursePrerequisite != null)
            {
                _context.CoursePrerequisites.Remove(coursePrerequisite);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CoursePrerequisiteExists(int id)
        {
            return _context.CoursePrerequisites.Any(e => e.CourseId == id);
        }
    }
}
