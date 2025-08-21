using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication8.Data;
using WebApplication8.Models;
using Microsoft.AspNetCore.Identity;
using WebApplication8.Controllers;
using Microsoft.AspNetCore.Authorization;


namespace WebApplication8.Controllers
{
    public class SchedulesController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly UserManager<IdentityUser> _userManager;

        public SchedulesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public IActionResult Index()
        {
            return View();
        }

    
        [HttpGet]
        [Authorize]
        public IActionResult GenerateSchedule()
        {
            var instructorId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(instructorId))
                return Json(new { success = false, message = "Instructor not logged in." });

            var instructorExists = _context.Users.Any(u => u.Id == instructorId);
            if (!instructorExists)
                return Json(new { success = false, message = "Instructor not found." });

            var availabilities = _context.Availabilities
                .Include(a => a.Shift)
                .Where(a => a.UserId == instructorId)
                .ToList();

            if (!availabilities.Any())
                return Json(new { success = false, message = "No availability set for instructor." });

            var loadedTime = _context.LoadedTimes.FirstOrDefault(l => l.UserId == instructorId);
            int maxHours = loadedTime?.HoursPerWeek ?? 20;

            var totalAvailableHours = availabilities.Sum(a => (a.Shift.EndTime - a.Shift.StartTime).TotalHours);

            // 🚨 If availability < loaded time, stop
            if (totalAvailableHours < maxHours)
            {
                return Json(new
                {
                    success = false,
                    message = $"Instructor's availability ({totalAvailableHours}h) does not cover required loaded time ({maxHours}h). Please adjust availability."
                });
            }

            var instructorCourses = _context.InstructorCourses
                .Where(ic => ic.UserId == instructorId)
                .Select(ic => ic.Course)
                .ToList();

            var allRooms = _context.Rooms.ToList();
            var existingClassesDb = _context.Classes
                .Where(c => c.UserId == instructorId)
                .Include(c => c.Course)
                .ToList();

            int assignedHours = existingClassesDb.Sum(c => c.Course.CreditNumber);
            int availableToSchedule = maxHours - assignedHours;

            var random = new Random();
            const int maxRetries = 20; // limit attempts to avoid infinite loop

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                var schedule = new List<GeneratedClassViewModel>();
                var existingClasses = new List<Class>(existingClassesDb); // work on a copy
                int totalScheduledHours = 0;

                // try to build schedule
                while (totalScheduledHours < availableToSchedule)
                {
                    bool anyCourseScheduled = false;

                    foreach (var course in instructorCourses.OrderBy(_ => random.Next()))
                    {
                        if (totalScheduledHours + course.CreditNumber > availableToSchedule)
                            continue;

                        var shuffledAvailability = availabilities.OrderBy(a => random.Next()).ToList();

                        foreach (var availability in shuffledAvailability)
                        {
                            var day = availability.DayOfWeek;
                            var shiftStart = availability.Shift.StartTime;
                            var shiftEnd = availability.Shift.EndTime;

                            var possibleStartTimes = new List<TimeSpan>();
                            for (var ts = shiftStart; ts.Add(TimeSpan.FromHours(course.CreditNumber)) <= shiftEnd; ts = ts.Add(TimeSpan.FromMinutes(30)))
                                possibleStartTimes.Add(ts);

                            foreach (var startTime in possibleStartTimes.OrderBy(_ => random.Next()))
                            {
                                var endTime = startTime.Add(TimeSpan.FromHours(course.CreditNumber));

                                // Check instructor conflict
                                bool instructorConflict = existingClasses.Any(c =>
                                    c.DayOfWeek == day &&
                                    TimeOverlaps(c.StartTime, c.StartTime.Add(TimeSpan.FromHours(c.Course.CreditNumber)), startTime, endTime));

                                if (instructorConflict) continue;

                                // Check available room
                                var availableRoom = allRooms.FirstOrDefault(room =>
                                {
                                    var roomClasses = _context.Classes
                                        .Where(c => c.RoomId == room.RoomId && c.DayOfWeek == day)
                                        .Include(c => c.Course)
                                        .ToList();

                                    return !roomClasses.Any(c =>
                                        TimeOverlaps(c.StartTime, c.StartTime.Add(TimeSpan.FromHours(c.Course.CreditNumber)), startTime, endTime));
                                });

                                if (availableRoom == null) continue;

                                // Assign class
                                schedule.Add(new GeneratedClassViewModel
                                {
                                    CourseId = course.CourseId,
                                    CourseName = course.Name,
                                    DayOfWeek = day,
                                    StartTime = startTime,
                                    RoomId = availableRoom.RoomId,
                                    RoomName = availableRoom.Name
                                });

                                existingClasses.Add(new Class
                                {
                                    Course = course,
                                    DayOfWeek = day,
                                    StartTime = startTime,
                                    RoomId = availableRoom.RoomId,
                                    UserId = instructorId
                                });

                                totalScheduledHours += course.CreditNumber;
                                anyCourseScheduled = true;
                                break;
                            }

                            if (anyCourseScheduled) break;
                        }

                        if (totalScheduledHours >= availableToSchedule) break;
                    }

                    if (!anyCourseScheduled) break;
                }

                // ✅ Success if full schedule covered
                if (totalScheduledHours == availableToSchedule)
                {
                    return Json(new { success = true, schedule });
                }

                // ❌ Otherwise retry
            }

            // 🚨 Failed after maxRetries
            return Json(new
            {
                success = false,
                message = $"Could not generate a full schedule after {maxRetries} attempts. Please try again later."
            });
        }



        private bool TimeOverlaps(TimeSpan start1, TimeSpan end1, TimeSpan start2, TimeSpan end2)
        {
            return !(end1 <= start2 || end2 <= start1);
        }

        [HttpPost]
        public IActionResult ConfirmSchedule([FromBody] List<GeneratedClassViewModel> schedule)
        {
            var instructorId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(instructorId))
                return Json(new { success = false, message = "Instructor not logged in." });

            if (schedule == null || !schedule.Any())
                return Json(new { success = false, message = "No schedule provided." });

            foreach (var item in schedule)
            {
                _context.Classes.Add(new Class
                {
                    CourseId = item.CourseId,
                    DayOfWeek = item.DayOfWeek,
                    StartTime = item.StartTime,
                    RoomId = item.RoomId,
                    UserId = instructorId
                });
            }
            _context.SaveChanges();

            return Json(new { success = true });
        }


        [Authorize] // ensure only logged-in instructors can access
        public async Task<IActionResult> MySchedule()
        {
            var userId = _userManager.GetUserId(User);

            var classes = await _context.Classes
                .Where(c => c.UserId == userId)
                .Include(c => c.Course)
                .Include(c => c.Room)
                .OrderBy(c => c.DayOfWeek)
                .ThenBy(c => c.StartTime)
                .ToListAsync();

            return View(classes);
        }
    }
}
