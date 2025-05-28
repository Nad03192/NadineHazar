using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebApplication8.Data;
using WebApplication8.Models;

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

            bool isConflict = _context.Classes.Any(c =>
                c.UserId == @class.UserId &&
                c.DayOfWeek == @class.DayOfWeek &&
                ((@class.StartTime >= c.StartTime && @class.StartTime < c.StartTime + TimeSpan.FromHours(c.Course.CreditNumber)) ||
                 (classEndTime > c.StartTime && classEndTime <= c.StartTime + TimeSpan.FromHours(c.Course.CreditNumber)))
            );

            if (isConflict)
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

            ViewData["UserId"] = new SelectList(
    _context.Users.Select(u => new { u.Id, Name = u.UserName }),
    "Id", "Name");

            ViewData["CourseId"] = new SelectList(
                _context.Courses.Select(c => new { c.CourseId, c.Name }),
                "CourseId", "Name");

            ViewData["RoomId"] = new SelectList(
                _context.Rooms.Select(r => new { r.RoomId, r.Name }),
                "RoomId", "Name");



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

        [HttpGet]
        public async Task<IActionResult> GetEligibleInstructors(int courseId, TimeSpan startTime, DayOfWeek dayOfWeek)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
                return Json(Enumerable.Empty<SelectListItem>());

            TimeSpan endTime = startTime + TimeSpan.FromHours(course.CreditNumber);

            var eligibleInstructors = await (from user in _context.Users
                                             join ic in _context.InstructorCourses on user.Id equals ic.UserId
                                             join a in _context.Availabilities on user.Id equals a.UserId
                                             join s in _context.Shifts on a.ShiftId equals s.ShiftId
                                             where ic.CourseId == courseId
                                                   && a.DayOfWeek == dayOfWeek
                                                   && s.StartTime <= startTime
                                                   && s.EndTime >= endTime
                                             where !_context.Classes.Any(c =>
                                                 c.UserId == user.Id &&
                                                 c.DayOfWeek == dayOfWeek &&
                                                 ((startTime >= c.StartTime && startTime < c.StartTime + TimeSpan.FromHours(c.Course.CreditNumber)) ||
                                                  (endTime > c.StartTime && endTime <= c.StartTime + TimeSpan.FromHours(c.Course.CreditNumber))))
                                             select new SelectListItem
                                             {
                                                 Value = user.Id,
                                                 Text = user.UserName
                                             }).Distinct().ToListAsync();

            return Json(eligibleInstructors);
        }


        public async Task<IActionResult> GenerateSchedule(string instructorId)
        {
            var instructor = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == instructorId);

            if (instructor == null)
                return NotFound("Instructor not found");

            // Get availabilities separately
            var availabilities = await _context.Availabilities
                .Include(a => a.Shift)
                .Where(a => a.UserId == instructorId)
                .ToListAsync();

            // Get courses assigned to instructor
            var instructorCourses = await _context.InstructorCourses
                .Include(ic => ic.Course)
                .Where(ic => ic.UserId == instructorId)
                .ToListAsync();

            // Get loaded time hours per week
            var loadedTime = await _context.LoadedTimes
                .Where(l => l.UserId == instructorId)
                .Select(l => l.HoursPerWeek)
                .FirstOrDefaultAsync();

            // Calculate total available hours from shifts
            double totalAvailableTime = availabilities.Sum(a =>
                (a.Shift.EndTime - a.Shift.StartTime).TotalHours);
            if (totalAvailableTime < loadedTime)
            {
                var errorModel = new ErrorViewModel
                {
                    ErrorMessage = "Instructor's availability is less than required load time.",
                    RequestId = HttpContext.TraceIdentifier
                };
                return View("Error", errorModel);
            }


            var schedules = new List<GeneratedClassViewModel>();
            var usedSlots = new List<(DayOfWeek day, TimeSpan start, TimeSpan end, int roomId)>();

            foreach (var ic in instructorCourses)
            {
                var course = ic.Course;
                double creditHours = course.CreditNumber;
                bool scheduled = false;

                foreach (var availability in availabilities)
                {
                    var shift = availability.Shift;
                    var day = availability.DayOfWeek;
                    var start = shift.StartTime;

                    while (start + TimeSpan.FromHours(creditHours) <= shift.EndTime)
                    {
                        var end = start + TimeSpan.FromHours(creditHours);

                        // Find available room
                        var availableRoom = await _context.Rooms.FirstOrDefaultAsync(room =>
                            !_context.Classes.Any(c =>
                                c.RoomId == room.RoomId &&
                                c.DayOfWeek == day &&
                                (
                                    (start >= c.StartTime && start < c.StartTime + TimeSpan.FromHours(c.Course.CreditNumber)) ||
                                    (end > c.StartTime && end <= c.StartTime + TimeSpan.FromHours(c.Course.CreditNumber))
                                )
                            )
                        );

                        if (availableRoom != null && !usedSlots.Any(s =>
                                s.day == day && s.start == start && s.end == end && s.roomId == availableRoom.RoomId))
                        {
                            schedules.Add(new GeneratedClassViewModel
                            {
                                CourseId = course.CourseId,
                                CourseName = course.Name,
                                RoomId = availableRoom.RoomId,
                                RoomName = availableRoom.Name,
                                StartTime = start,
                                DayOfWeek = day
                            });

                            usedSlots.Add((day, start, end, availableRoom.RoomId));
                            scheduled = true;
                            break;
                        }

                        start = start.Add(TimeSpan.FromMinutes(30)); // try next 30min slot
                    }

                    if (scheduled)
                        break;
                }

                if (!scheduled)
                {
                    ViewBag.Error = $"Could not schedule course: {course.Name}";
                    return View("Error");
                }
            }

            ViewBag.InstructorId = instructorId;
            return View("ConfirmSchedule", schedules);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmSchedule(string instructorId, List<GeneratedClassViewModel> schedule)
        {
            foreach (var item in schedule)
            {
                var @class = new Class
                {
                    UserId = instructorId,
                    CourseId = item.CourseId,
                    RoomId = item.RoomId,
                    DayOfWeek = item.DayOfWeek,
                    StartTime = item.StartTime
                };

                _context.Classes.Add(@class);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }




    }
}
