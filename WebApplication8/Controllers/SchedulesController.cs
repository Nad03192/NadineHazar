using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication8.Data;
using WebApplication8.Models;
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

            var availabilities = _context.Availabilities
                .Include(a => a.Shift)
                .Where(a => a.UserId == instructorId)
                .ToList();

            if (!availabilities.Any())
                return Json(new { success = false, message = "No availability set for instructor." });

            var loadedTime = _context.LoadedTimes.FirstOrDefault(l => l.UserId == instructorId);
            int maxHours = loadedTime?.HoursPerWeek ?? 20;

            var totalAvailableHours = availabilities.Sum(a => (a.Shift.EndTime - a.Shift.StartTime).TotalHours);
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
            const int maxRetries = 20;

            var roomSchedulesByDay = _context.Classes
                .Include(c => c.Course)
                .GroupBy(c => new { c.RoomId, c.DayOfWeek })
                .ToDictionary(g => g.Key, g => g.ToList());

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                var schedule = new List<GeneratedClassViewModel>();
                var existingClasses = new List<Class>(existingClassesDb);
                int totalScheduledHours = 0;

                // Shuffle courses to randomize order
                var schedulableCourses = instructorCourses.OrderBy(_ => random.Next()).ToList();

                foreach (var course in schedulableCourses)
                {
                    // Try scheduling this course as many times as possible until we reach availableToSchedule
                    while (totalScheduledHours + course.CreditNumber <= availableToSchedule)
                    {
                        bool scheduled = false;

                        foreach (var availability in availabilities.OrderBy(_ => random.Next()))
                        {
                            var day = availability.DayOfWeek;
                            var shiftStart = availability.Shift.StartTime;
                            var shiftEnd = availability.Shift.EndTime;

                            // Generate all possible start times in 30-minute steps
                            for (var ts = shiftStart; ts.Add(TimeSpan.FromHours(course.CreditNumber)) <= shiftEnd; ts = ts.Add(TimeSpan.FromMinutes(30)))
                            {
                                var endTime = ts.Add(TimeSpan.FromHours(course.CreditNumber));

                                bool instructorConflict = existingClasses.Any(c =>
                                    c.DayOfWeek == day &&
                                    TimeOverlaps(c.StartTime, c.StartTime.Add(TimeSpan.FromHours(c.Course.CreditNumber)), ts, endTime));

                                if (instructorConflict)
                                    continue;

                                var availableRoom = allRooms.FirstOrDefault(room =>
                                {
                                    var key = new { RoomId = room.RoomId, DayOfWeek = day };
                                    if (!roomSchedulesByDay.TryGetValue(key, out var roomClasses))
                                        return true;

                                    return !roomClasses.Any(c =>
                                        TimeOverlaps(c.StartTime, c.StartTime.Add(TimeSpan.FromHours(c.Course.CreditNumber)), ts, endTime));
                                });

                                if (availableRoom == null)
                                    continue;

                                // Schedule the course
                                schedule.Add(new GeneratedClassViewModel
                                {
                                    CourseId = course.CourseId,
                                    CourseName = course.Name,
                                    DayOfWeek = day,
                                    StartTime = ts,
                                    RoomId = availableRoom.RoomId,
                                    RoomName = availableRoom.Name
                                });

                                existingClasses.Add(new Class
                                {
                                    Course = course,
                                    DayOfWeek = day,
                                    StartTime = ts,
                                    RoomId = availableRoom.RoomId,
                                    UserId = instructorId
                                });

                                totalScheduledHours += course.CreditNumber;
                                scheduled = true;
                                break; // stop checking this availability once scheduled
                            }

                            if (scheduled)
                                break;
                        }

                        if (!scheduled)
                            break; // cannot schedule this course anymore
                    }
                }

                if (totalScheduledHours == availableToSchedule)
                {
                    return Json(new { success = true, schedule });
                }
            }

            return Json(new
            {
                success = false,
                partial = true,
                message = $"Could not generate a full schedule after {maxRetries} attempts."
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

            // Get the semester with SubmitClassesEnd nearest to today (future)
            var now = DateTime.Now;
            var nearestSemester = _context.Semesters
                .Where(s => s.SubmitClassesEnd >= now) // only future or ongoing
                .OrderBy(s => s.SubmitClassesEnd)
                .FirstOrDefault();

            if (nearestSemester == null)
                return Json(new { success = false, message = "No semester available for scheduling." });

            foreach (var item in schedule)
            {
                _context.Classes.Add(new Class
                {
                    CourseId = item.CourseId,
                    DayOfWeek = item.DayOfWeek,
                    StartTime = item.StartTime,
                    RoomId = item.RoomId,
                    UserId = instructorId,
                    SemesterId = nearestSemester.SemesterId // assign nearest semester
                });
            }
            _context.SaveChanges();

            return Json(new { success = true, semester = nearestSemester.Name });
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
