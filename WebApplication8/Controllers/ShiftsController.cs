using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication8.Data;
using WebApplication8.Models;

namespace WebApplication8.Controllers
{
    public class ShiftsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShiftsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Shifts
        // GET: Shifts
        public async Task<IActionResult> Index(string sortOrder)
        {
            ViewData["StartTimeSortParm"] = String.IsNullOrEmpty(sortOrder) ? "start_desc" : "";
            ViewData["EndTimeSortParm"] = sortOrder == "end_asc" ? "end_desc" : "end_asc";

            var shifts = from s in _context.Shifts
                         select s;

            switch (sortOrder)
            {
                case "start_desc":
                    shifts = shifts.OrderByDescending(s => s.StartTime);
                    break;
                case "end_asc":
                    shifts = shifts.OrderBy(s => s.EndTime);
                    break;
                case "end_desc":
                    shifts = shifts.OrderByDescending(s => s.EndTime);
                    break;
                default: // start_asc
                    shifts = shifts.OrderBy(s => s.StartTime);
                    break;
            }

            return View(await shifts.AsNoTracking().ToListAsync());
        }


        // GET: Shifts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shift = await _context.Shifts
                .FirstOrDefaultAsync(m => m.ShiftId == id);
            if (shift == null)
            {
                return NotFound();
            }

            return View(shift);
        }

        // GET: Shifts/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Shifts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StartTime,EndTime")] Shift shift)
        {
            if (!ModelState.IsValid)
                return View(shift);

            if (shift.StartTime >= shift.EndTime)
            {
                ModelState.AddModelError("", "Start time must be earlier than end time.");
                return View(shift);
            }

            // Check if a shift already exists
            var existing = await _context.Shifts
                .FirstOrDefaultAsync(s => s.StartTime == shift.StartTime && s.EndTime == shift.EndTime);

            if (existing != null)
            {
                // ❌ Stay on page and show error
                ModelState.AddModelError("", $"Shift from {shift.StartTime:hh\\:mm} to {shift.EndTime:hh\\:mm} already exists.");
                return View(shift);
            }

            // Add new shift
            _context.Shifts.Add(shift);
            await _context.SaveChangesAsync();

            // Optional: success message
            ViewBag.Message = $"Shift from {shift.StartTime:hh\\:mm} to {shift.EndTime:hh\\:mm} added successfully.";

            return View(shift); // stay on the page
        }



        // GET: Shifts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shift = await _context.Shifts.FindAsync(id);
            if (shift == null)
            {
                return NotFound();
            }
            return View(shift);
        }

        // POST: Shifts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ShiftId,StartTime,EndTime")] Shift shift)
        {
            if (id != shift.ShiftId)
                return NotFound();

            if (shift.StartTime >= shift.EndTime)
            {
                ModelState.AddModelError("", "Start time must be earlier than end time.");
                return View(shift); // stay on page if error
            }

            // Check for duplicate shifts (excluding current shift)
            var existing = await _context.Shifts
                .FirstOrDefaultAsync(s => s.ShiftId != shift.ShiftId
                                       && s.StartTime == shift.StartTime
                                       && s.EndTime == shift.EndTime);

            if (existing != null)
            {
                ModelState.AddModelError("", $"Shift from {shift.StartTime:hh\\:mm} to {shift.EndTime:hh\\:mm} already exists.");
                return View(shift); // stay on page if duplicate
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(shift);
                    await _context.SaveChangesAsync();

                    // ✅ redirect to Index on success
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ShiftExists(shift.ShiftId))
                        return NotFound();
                    else
                        throw;
                }
            }

            return View(shift); // fallback
        }

        // GET: Shifts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shift = await _context.Shifts
                .FirstOrDefaultAsync(m => m.ShiftId == id);
            if (shift == null)
            {
                return NotFound();
            }

            return View(shift);
        }

        // POST: Shifts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var shift = await _context.Shifts.FindAsync(id);
            if (shift != null)
            {
                _context.Shifts.Remove(shift);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ShiftExists(int id)
        {
            return _context.Shifts.Any(e => e.ShiftId == id);
        }



    }
}
