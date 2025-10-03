using Microsoft.AspNetCore.Authorization;
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
        public async Task<IActionResult> Index(string search, int? shiftId, DayOfWeek? dayOfWeek)
        {
            var availabilities = _context.Availabilities
                .Include(a => a.Shift)
                .Include(a => a.User)
                .AsQueryable();

            // Filter by User Email search
            if (!string.IsNullOrEmpty(search))
            {
                availabilities = availabilities.Where(a =>
                    a.User != null && a.User.Email.Contains(search));
            }

            // Filter by Shift
            if (shiftId.HasValue)
                availabilities = availabilities.Where(a => a.ShiftId == shiftId.Value);

            // Filter by DayOfWeek
            if (dayOfWeek.HasValue)
                availabilities = availabilities.Where(a => a.DayOfWeek == dayOfWeek.Value);

            // Prepare Shift dropdown
            var shifts = await _context.Shifts.ToListAsync();
            ViewBag.Shifts = new SelectList(
                shifts.Select(s => new
                {
                    s.ShiftId,
                    Text = s.StartTime.ToString(@"hh\:mm") + " - " + s.EndTime.ToString(@"hh\:mm")
                }),
                "ShiftId",
                "Text",
                shiftId
            );

            // Prepare DayOfWeek dropdown
            var days = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>().ToList();
            ViewBag.DaysOfWeek = new SelectList(days, dayOfWeek);

            // Preserve search text in the view
            ViewBag.Search = search;

            return View(await availabilities.ToListAsync());
        }

        // Other CRUD actions...
    

        // GET: Availabilities/Create
        public async Task<IActionResult> Create()
        {
            await PopulateViewDataAsync();
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AvailabilityId,ShiftId,UserId,DayOfWeek")] Availability availability)
        {
            if (ModelState.IsValid)
            {
                var shift = await _context.Shifts.FindAsync(availability.ShiftId);
                if (shift == null)
                {
                    ModelState.AddModelError("", "Selected shift not found.");
                    await PopulateViewDataAsync(availability.ShiftId, availability.UserId);
                    return View(availability);
                }

                // 1. Check overlapping shifts for same user on the same day
                var userAvailabilities = await _context.Availabilities
                    .Include(a => a.Shift)
                    .Where(a => a.UserId == availability.UserId && a.DayOfWeek == availability.DayOfWeek)
                    .ToListAsync();

                bool overlapExists = userAvailabilities.Any(a =>
                    a.Shift.StartTime < shift.EndTime && shift.StartTime < a.Shift.EndTime);

                if (overlapExists)
                {
                    ModelState.AddModelError("", "This shift overlaps with an existing availability for this user on the same day.");
                    await PopulateViewDataAsync(availability.ShiftId, availability.UserId);
                    return View(availability);
                }

                // 2. Check total assigned hours against loaded hours (manual calculation)
                var userLoadedTime = await _context.LoadedTimes
                    .Where(l => l.UserId == availability.UserId)
                    .Select(l => l.HoursPerWeek)
                    .FirstOrDefaultAsync();

                double totalAssignedHours = userAvailabilities
                    .Sum(a => (a.Shift.EndTime - a.Shift.StartTime).TotalHours);

                double newShiftHours = (shift.EndTime - shift.StartTime).TotalHours;

                if (totalAssignedHours + newShiftHours > userLoadedTime)
                {
                    ModelState.AddModelError("", "Cannot add this shift. Total assigned hours would exceed loaded hours.");
                    await PopulateViewDataAsync(availability.ShiftId, availability.UserId);
                    return View(availability);
                }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AvailabilityId,ShiftId,UserId,DayOfWeek")] Availability availability)
        {
            if (id != availability.AvailabilityId) return NotFound();

            if (ModelState.IsValid)
            {
                var shift = await _context.Shifts.FindAsync(availability.ShiftId);
                if (shift == null)
                {
                    ModelState.AddModelError("", "Selected shift not found.");
                    await PopulateViewDataAsync(availability.ShiftId, availability.UserId);
                    return View(availability);
                }

                // Get existing availabilities excluding the current one
                var userAvailabilities = await _context.Availabilities
                    .Include(a => a.Shift)
                    .Where(a => a.UserId == availability.UserId && a.AvailabilityId != availability.AvailabilityId)
                    .ToListAsync();

                // 1. Check overlapping shifts
                bool overlapExists = userAvailabilities
                    .Where(a => a.DayOfWeek == availability.DayOfWeek)
                    .Any(a => a.Shift.StartTime < shift.EndTime && shift.StartTime < a.Shift.EndTime);

                if (overlapExists)
                {
                    ModelState.AddModelError("", "This shift overlaps with an existing availability for this user on the same day.");
                    await PopulateViewDataAsync(availability.ShiftId, availability.UserId);
                    return View(availability);
                }

                // 2. Check total hours manually
                var userLoadedTime = await _context.LoadedTimes
                    .Where(l => l.UserId == availability.UserId)
                    .Select(l => l.HoursPerWeek)
                    .FirstOrDefaultAsync();

                double totalAssignedHours = userAvailabilities
                    .Sum(a => (a.Shift.EndTime - a.Shift.StartTime).TotalHours);

                double newShiftHours = (shift.EndTime - shift.StartTime).TotalHours;

                if (totalAssignedHours + newShiftHours > userLoadedTime)
                {
                    ModelState.AddModelError("", "Cannot add this shift. Total assigned hours would exceed loaded hours.");
                    await PopulateViewDataAsync(availability.ShiftId, availability.UserId);
                    return View(availability);
                }

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
            // Users
            var instructors = await GetInstructorsAsync();
            ViewBag.Users = new SelectList(instructors, "Id", "Email", selectedUserId);

            // Days
            var daysOfWeek = Enum.GetValues(typeof(DayOfWeek))
                                 .Cast<DayOfWeek>()
                                 .Select(d => new { Value = (int)d, Text = d.ToString() })
                                 .ToList();
            ViewBag.DayOfWeekList = new SelectList(daysOfWeek, "Value", "Text", selectedDayOfWeek);
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


        public async Task<IActionResult> EnterWeeklyAvailability()
        {
            var allShifts = await _context.Shifts
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            var userId = _userManager.GetUserId(User);

            // Get all existing availabilities for this user
            var existingAvailabilities = await _context.Availabilities
                .Where(a => a.UserId == userId)
                .ToListAsync();

            var model = new AvailabilityFormViewModel
            {
                WeekAvailability = Enum.GetValues(typeof(DayOfWeek))
                    .Cast<DayOfWeek>()
                    .Select(day => new DayShiftSelection
                    {
                        Day = day,
                        AvailableShifts = allShifts,
                        SelectedShiftIds = existingAvailabilities
                            .Where(a => a.DayOfWeek == day)
                            .Select(a => a.ShiftId)
                            .ToList()
                    }).ToList()
            };

            var loadedTime = await _context.LoadedTimes.FirstOrDefaultAsync(l => l.UserId == userId);
            ViewBag.LoadedTimeMinutes = loadedTime?.HoursPerWeek * 60 ?? 0;

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnterWeeklyAvailability(AvailabilityFormViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            // Rehydrate AvailableShifts for view in case of validation errors
            var allShifts = await _context.Shifts.ToListAsync();
            foreach (var day in model.WeekAvailability)
            {
                day.AvailableShifts ??= allShifts;
                day.SelectedShiftIds ??= new List<int>();
            }

            // Optional custom shift
            Shift? customShift = null;
            if (model.CustomStart.HasValue && model.CustomEnd.HasValue)
            {
                customShift = new Shift
                {
                    StartTime = model.CustomStart.Value,
                    EndTime = model.CustomEnd.Value
                };
                _context.Shifts.Add(customShift);
                await _context.SaveChangesAsync();
                _context.Entry(customShift).Reload();
            }

            var selectedAvailabilities = new List<Availability>();

            // Collect standard selected shifts
            foreach (var day in model.WeekAvailability)
            {
                foreach (var shiftId in day.SelectedShiftIds)
                {
                    selectedAvailabilities.Add(new Availability
                    {
                        UserId = userId,
                        ShiftId = shiftId,
                        DayOfWeek = day.Day
                    });
                }
            }

            // Add custom shift
            if (customShift != null && model.CustomShiftDays != null)
            {
                foreach (var day in model.CustomShiftDays)
                {
                    selectedAvailabilities.Add(new Availability
                    {
                        UserId = userId,
                        ShiftId = customShift.ShiftId,
                        DayOfWeek = day
                    });
                }
            }

            // Remove old availability and save new
            _context.Availabilities.RemoveRange(_context.Availabilities.Where(a => a.UserId == userId));
            _context.Availabilities.AddRange(selectedAvailabilities);
            await _context.SaveChangesAsync();

            return RedirectToAction("Home", "Instructor");


        }


        [Authorize(Roles = "Instructor")]
        [HttpPost]
        public async Task<IActionResult> AddInstructorShift(TimeSpan startTime, TimeSpan endTime)
        {
            if (startTime >= endTime)
                return Content("Start time must be before end time.");

            var exists = await _context.Shifts
                .AnyAsync(s => s.StartTime == startTime && s.EndTime == endTime);

            if (exists)
                return Content("That shift already exists.");
            var maxTime = new TimeSpan(22, 0, 0); // 10 PM
            if (startTime > maxTime || endTime > maxTime)
                return Content("Shifts cannot start or end after 10 PM.");

            _context.Shifts.Add(new Shift { StartTime = startTime, EndTime = endTime });
            await _context.SaveChangesAsync();

            return Content("Shift added successfully.");
        }
        [HttpGet]
        public async Task<JsonResult> GetAvailableShifts(string userId, int? dayOfWeek)
        {
            if (string.IsNullOrEmpty(userId))
                return Json(new List<object>());

            // Get user's loaded hours
            var userLoadedTime = await _context.LoadedTimes
                .Where(l => l.UserId == userId)
                .Select(l => l.HoursPerWeek)
                .FirstOrDefaultAsync();

            // Get all shifts
            var allShifts = await _context.Shifts.ToListAsync();

            // Get all existing availabilities for this user
            var userAvailabilities = await _context.Availabilities
                .Include(a => a.Shift)
                .Where(a => a.UserId == userId)
                .ToListAsync();

            // Calculate total assigned hours
            double assignedHours = userAvailabilities.Sum(a => (a.Shift.EndTime - a.Shift.StartTime).TotalHours);

            // Remaining hours
            double remainingHours = userLoadedTime - assignedHours;
            if (remainingHours <= 0)
                return Json(new List<object>()); // No hours left

            // Filter existing availabilities by selected day
            var dayAvailabilities = dayOfWeek.HasValue
                ? userAvailabilities.Where(a => a.DayOfWeek == (DayOfWeek)dayOfWeek.Value).ToList()
                : new List<Availability>();

            // Filter shifts: no overlap and duration <= remainingHours
            var availableShifts = allShifts
                .Where(s =>
                    !dayAvailabilities.Any(a => a.Shift.StartTime < s.EndTime && s.StartTime < a.Shift.EndTime) &&
                    (s.EndTime - s.StartTime).TotalHours <= remainingHours
                )
                .Select(s => new
                {
                    s.ShiftId,
                    Text = $"{s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm}"
                })
                .ToList();

            return Json(availableShifts);
        }




    }
}
