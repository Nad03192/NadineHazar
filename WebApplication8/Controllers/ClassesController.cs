using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebApplication8.Data;
using WebApplication8.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication8.Controllers
{
    public class ClassesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClassesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var classes = _context.Classes
                .Include(c => c.Course)
                .Include(c => c.Room)
                .Include(c => c.User);
            return View(await classes.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var @class = await _context.Classes
                .Include(c => c.Course)
                .Include(c => c.Room)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.ClassId == id);

            if (@class == null) return NotFound();

            return View(@class);
        }

        public IActionResult Create()
        {
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name");
            ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "Name");
            ViewData["UserId"] = new SelectList(Enumerable.Empty<SelectListItem>());
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClassId,UserId,CourseId,RoomId,StartTime,DayOfWeek")] Class @class)
        {
            var course = await _context.Courses.FindAsync(@class.CourseId);
            if (course == null)
            {
                ModelState.AddModelError("CourseId", "Invalid course.");
            }

            var classEndTime = @class.StartTime + TimeSpan.FromHours(course.CreditNumber);

            if (await HasSchedulingConflict(@class.UserId, @class.DayOfWeek, @class.StartTime, classEndTime))
            {
                ModelState.AddModelError("UserId", "This instructor has a conflicting class at the selected time.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(@class);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", @class.CourseId);
            ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "Name", @class.RoomId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", @class.UserId);
            return View(@class);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var @class = await _context.Classes.FindAsync(id);
            if (@class == null) return NotFound();

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", @class.CourseId);
            ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "Name", @class.RoomId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", @class.UserId);
            return View(@class);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ClassId,UserId,CourseId,RoomId,StartTime,DayOfWeek")] Class @class)
        {
            if (id != @class.ClassId) return NotFound();

            var course = await _context.Courses.FindAsync(@class.CourseId);
            if (course == null)
            {
                ModelState.AddModelError("CourseId", "Invalid course.");
            }

            var classEndTime = @class.StartTime + TimeSpan.FromHours(course.CreditNumber);

            if (await HasSchedulingConflict(@class.UserId, @class.DayOfWeek, @class.StartTime, classEndTime, @class.ClassId))
            {
                ModelState.AddModelError("UserId", "This instructor has a conflicting class at the selected time.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(@class);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClassExists(@class.ClassId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", @class.CourseId);
            ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "Name", @class.RoomId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", @class.UserId);
            return View(@class);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var @class = await _context.Classes
                .Include(c => c.Course)
                .Include(c => c.Room)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.ClassId == id);

            if (@class == null) return NotFound();

            return View(@class);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var @class = await _context.Classes.FindAsync(id);
            if (@class != null)
            {
                _context.Classes.Remove(@class);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ClassExists(int id)
        {
            return _context.Classes.Any(e => e.ClassId == id);
        }

        private async Task<bool> HasSchedulingConflict(string userId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime, int? classId = null)
        {
            var classes = await _context.Classes
                .Include(c => c.Course)
                .Where(c => c.UserId == userId && c.DayOfWeek == dayOfWeek && (classId == null || c.ClassId != classId))
                .ToListAsync();

            return classes.Any(c =>
            {
                var existingStart = c.StartTime;
                var existingEnd = existingStart + TimeSpan.FromHours(c.Course.CreditNumber);

                return startTime < existingEnd && endTime > existingStart;
            });
        }

        [HttpGet]
        public IActionResult GetEligibleInstructors(int courseId, string newStartTime, DayOfWeek dayOfWeek)
        {
            // Validate input time format
            if (!TimeSpan.TryParse(newStartTime, out TimeSpan startTime))
                return Json(new List<SelectListItem>());

            // Get course credits
            int creditHours = _context.Courses
                .Where(c => c.CourseId == courseId)
                .Select(c => c.CreditNumber)
                .FirstOrDefault();

            if (creditHours <= 0)
                return Json(new List<SelectListItem>());

            // Calculate new class end time safely (cap at 24h)
            TimeSpan duration = TimeSpan.FromHours(creditHours);
            TimeSpan newEndTime = startTime + duration;
            if (newEndTime.TotalHours > 24)
                newEndTime = TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(59)).Add(TimeSpan.FromSeconds(59));

            // Get instructors who teach this course
            var instructorUserIds = _context.InstructorCourses
                .Where(ic => ic.CourseId == courseId)
                .Select(ic => ic.UserId)
                .ToList();

            if (!instructorUserIds.Any())
                return Json(new List<SelectListItem>());

            // Get instructors available at that day/time
            var availableInstructorIds = _context.Availabilities
                .Include(a => a.Shift)
                .Where(a =>
                    instructorUserIds.Contains(a.UserId) &&
                    a.DayOfWeek == dayOfWeek &&
                    a.Shift != null &&
                    a.Shift.StartTime <= startTime &&
                    a.Shift.EndTime >= newEndTime)
                .Select(a => a.UserId)
                .Distinct()
                .ToList();

            if (!availableInstructorIds.Any())
                return Json(new List<SelectListItem>());

            // Find instructors who have conflicting classes at the same time
            var conflictingInstructorIds = _context.Classes
                .Include(c => c.Course)
                .Where(c => c.DayOfWeek == dayOfWeek && c.Course != null)
                .AsEnumerable() // switch to in-memory for TimeSpan calc
                .Where(c =>
                {
                    TimeSpan existingStart = c.StartTime;
                    TimeSpan existingEnd = existingStart + TimeSpan.FromHours(c.Course.CreditNumber);
                    // Check overlap: (start < existingEnd) && (newEnd > existingStart)
                    return startTime < existingEnd && newEndTime > existingStart;
                })
                .Select(c => c.UserId)
                .Distinct()
                .ToList();

            // Eligible instructors are those available and not conflicting
            var eligibleUserIds = availableInstructorIds.Except(conflictingInstructorIds).ToList();

            var eligibleInstructors = _context.Users
                .Where(u => eligibleUserIds.Contains(u.Id))
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = u.UserName
                })
                .ToList();

            return Json(eligibleInstructors);
        }


        [HttpGet]
        public IActionResult GetAvailableRooms(int courseId, string newStartTime, string dayOfWeekStr)
        {
            // Validate input
            if (courseId == 0 || string.IsNullOrEmpty(newStartTime) || string.IsNullOrEmpty(dayOfWeekStr))
                return Json(new List<SelectListItem>());

            if (!TimeSpan.TryParse(newStartTime, out TimeSpan startTime) ||
                !Enum.TryParse<DayOfWeek>(dayOfWeekStr, out DayOfWeek dayOfWeek))
            {
                return Json(new List<SelectListItem>());
            }

            int creditHours = _context.Courses
                .Where(c => c.CourseId == courseId)
                .Select(c => c.CreditNumber)
                .FirstOrDefault();

            if (creditHours <= 0)
                return Json(new List<SelectListItem>());

            // Calculate end time safely capped at 24h
            TimeSpan duration = TimeSpan.FromHours(creditHours);
            TimeSpan newEndTime = startTime + duration;
            if (newEndTime.TotalHours > 24)
                newEndTime = TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(59)).Add(TimeSpan.FromSeconds(59));

            var allRooms = _context.Rooms.ToList();

            // Get classes booked on the same day, with course info
            var classesOnSameDay = _context.Classes
                .Include(c => c.Course)
                .Where(c => c.DayOfWeek == dayOfWeek && c.RoomId != 0 && c.Course != null)
                .ToList();

            var unavailableRoomIds = new HashSet<int>();

            foreach (var bookedClass in classesOnSameDay)
            {
                TimeSpan bookedStart = bookedClass.StartTime;
                TimeSpan bookedEnd = bookedStart + TimeSpan.FromHours(bookedClass.Course.CreditNumber);

                // Overlap check:
                bool isOverlap = startTime < bookedEnd && newEndTime > bookedStart;

                if (isOverlap)
                {
                    unavailableRoomIds.Add(bookedClass.RoomId);
                }
            }

            var availableRooms = allRooms
                .Where(r => !unavailableRoomIds.Contains(r.RoomId))
                .Select(r => new SelectListItem
                {
                    Value = r.RoomId.ToString(),
                    Text = r.Name
                })
                .ToList();

            return Json(availableRooms);
        }





    }
}
