using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication8.Data;
using WebApplication8.Models;

namespace WebApplication8.Controllers
{
    public class AvailabilitiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AvailabilitiesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        // GET: Availabilities
        public async Task<IActionResult> Index()
        {
            var availabilities = _context.Availabilities
                .Include(a => a.Shift)
                .Include(a => a.User);
            return View(await availabilities.ToListAsync());
        }

        // GET: Availabilities/Create
        public async Task<IActionResult> Create()
        {
            await PopulateViewDataAsync();
            return View();
        }

        // POST: Availabilities/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AvailabilityId,ShiftId,UserId,DayOfWeek")] Availability availability)
        {
            if (ModelState.IsValid)
            {
                _context.Add(availability);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await PopulateViewDataAsync(availability.ShiftId, availability.UserId);
            return View(availability);
        }

        // GET: Availabilities/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var availability = await _context.Availabilities.FindAsync(id);
            if (availability == null) return NotFound();

            await PopulateViewDataAsync(availability.ShiftId, availability.UserId);
            return View(availability);
        }

        // POST: Availabilities/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AvailabilityId,ShiftId,UserId,DayOfWeek")] Availability availability)
        {
            if (id != availability.AvailabilityId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(availability);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AvailabilityExists(availability.AvailabilityId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateViewDataAsync(availability.ShiftId, availability.UserId);
            return View(availability);
        }

        // GET: Availabilities/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var availability = await _context.Availabilities
                .Include(a => a.Shift)
                .Include(a => a.User)
                .FirstOrDefaultAsync(m => m.AvailabilityId == id);

            if (availability == null) return NotFound();

            return View(availability);
        }

        // POST: Availabilities/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var availability = await _context.Availabilities.FindAsync(id);
            if (availability != null)
            {
                _context.Availabilities.Remove(availability);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool AvailabilityExists(int id)
        {
            return _context.Availabilities.Any(e => e.AvailabilityId == id);
        }

        // Helper: Populate ViewData for SelectLists
        private async Task PopulateViewDataAsync(int? selectedShiftId = null, string selectedUserId = null, DayOfWeek? selectedDayOfWeek = null)
        {
            // Prepare Shifts SelectList with display string of start and end time (since no Name property)
            var shifts = await _context.Shifts
                .Select(s => new
                {
                    s.ShiftId,
                    Display = $"{s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm}"
                })
                .ToListAsync();

            ViewData["ShiftId"] = new SelectList(shifts, "ShiftId", "Display", selectedShiftId);

            // Prepare Users in Instructor role who do NOT already have an availability (optional: you can filter differently if needed)
            var instructors = await GetInstructorsAsync();

            ViewData["UserId"] = new SelectList(instructors, "Id", "Email", selectedUserId);

            // Prepare DayOfWeek SelectList (int values and string names)
            var daysOfWeek = Enum.GetValues(typeof(DayOfWeek))
                                 .Cast<DayOfWeek>()
                                 .Select(d => new
                                 {
                                     Value = (int)d,
                                     Text = d.ToString()
                                 })
                                 .ToList();

            ViewData["DayOfWeek"] = new SelectList(daysOfWeek, "Value", "Text", selectedDayOfWeek);
        }


        // Helper method to get users in "Instructor" role
        private async Task<List<IdentityUser>> GetInstructorsAsync()
        {
            var instructorRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Instructor");
            if (instructorRole == null)
                return new List<IdentityUser>();

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
