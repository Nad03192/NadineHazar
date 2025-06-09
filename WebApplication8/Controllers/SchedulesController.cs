using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication8.Data;
using WebApplication8.Models;

namespace WebApplication8.Controllers
{
    public class SchedulesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SchedulesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var instructors = _context.Users
                .Select(u => new SelectListItem { Value = u.Id, Text = u.UserName })
                .ToList();

            ViewData["Instructors"] = instructors;
            return View();
        }
        [HttpGet]
        public IActionResult GenerateSchedule(string instructorId)
        {
            if (string.IsNullOrEmpty(instructorId))
                return Json(new { success = false, message = "InstructorId is required" });

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

            var instructorCourses = _context.InstructorCourses
                .Where(ic => ic.UserId == instructorId)
                .Select(ic => ic.Course)
                .ToList();

            var allRooms = _context.Rooms.ToList();
            var existingClasses = _context.Classes
                .Where(c => c.UserId == instructorId)
                .Include(c => c.Course)
                .ToList();

            int assignedHours = existingClasses.Sum(c => c.Course.CreditNumber);
            int totalScheduledHours = 0;
            int availableToSchedule = maxHours - assignedHours;

            var schedule = new List<GeneratedClassViewModel>();
            var random = new Random();
            bool roomConflictOccurred = false;

            while (totalScheduledHours < availableToSchedule)
            {
                bool anyCourseScheduled = false;

                foreach (var course in instructorCourses)
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
                        {
                            possibleStartTimes.Add(ts);
                        }

                        foreach (var startTime in possibleStartTimes.OrderBy(_ => random.Next()))
                        {
                            var endTime = startTime.Add(TimeSpan.FromHours(course.CreditNumber));

                            bool instructorConflict = existingClasses.Any(c =>
                                c.DayOfWeek == day &&
                                TimeOverlaps(c.StartTime, c.StartTime.Add(TimeSpan.FromHours(c.Course.CreditNumber)), startTime, endTime));

                            if (instructorConflict) continue;

                            var availableRoom = allRooms.FirstOrDefault(room =>
                            {
                                var roomClasses = _context.Classes
                                    .Where(c => c.RoomId == room.RoomId && c.DayOfWeek == day)
                                    .Include(c => c.Course)
                                    .ToList();

                                return !roomClasses.Any(c =>
                                    TimeOverlaps(c.StartTime, c.StartTime.Add(TimeSpan.FromHours(c.Course.CreditNumber)), startTime, endTime));
                            });

                            if (availableRoom == null)
                            {
                                roomConflictOccurred = true;
                                continue;
                            }

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

            int remainingHours = availableToSchedule - totalScheduledHours;
            string notice = null;

            if (remainingHours > 0)
            {
                var minCourseCredit = instructorCourses.Min(c => c.CreditNumber);
                bool tooSmallToFitCourse = remainingHours < minCourseCredit;
                bool notEnoughAvailability = availabilities.Sum(a => (a.Shift.EndTime - a.Shift.StartTime).TotalHours) < maxHours;

                var reasons = new List<string>();

                if (tooSmallToFitCourse)
                    reasons.Add($"remaining {remainingHours} hour(s) is less than the smallest course credit ({minCourseCredit}).");

                if (notEnoughAvailability)
                    reasons.Add("instructor's availability is less than loaded time.");

                if (roomConflictOccurred)
                    reasons.Add("no rooms were available during some available time slots.");

                notice = $"Schedule generated but {remainingHours} hour(s) remain unassigned from the loaded time because " + string.Join(" and ", reasons);
            }

            return Json(new { success = true, schedule, notice });
        }


        private bool TimeOverlaps(TimeSpan start1, TimeSpan end1, TimeSpan start2, TimeSpan end2)
        {
            return !(end1 <= start2 || end2 <= start1);
        }

       

        [HttpPost]
        public IActionResult ConfirmSchedule([FromBody] ScheduleRequest request)
        {
            var schedule = request.Schedule;
            var instructorId = request.InstructorId;

            if (string.IsNullOrEmpty(instructorId) || schedule == null || !schedule.Any())
                return Json(new { success = false, message = "Invalid data. Please select an instructor and provide a schedule." });

            var errorMessages = new List<string>();

            foreach (var item in schedule)
            {
                if (item.CourseId == 0) errorMessages.Add($"Missing Course ID for entry with Start Time {item.StartTime}.");
                if (item.RoomId == 0) errorMessages.Add($"Missing Room ID for entry with Start Time {item.StartTime}.");
                if (item.StartTime == default) errorMessages.Add($"Missing Start Time for Course {item.CourseName}.");
            }

            if (errorMessages.Any())
                return Json(new { success = false, message = "Validation failed.", errors = errorMessages });

            try
            {
                foreach (var item in schedule)
                {
                    var newClass = new Class
                    {
                        CourseId = item.CourseId,
                        DayOfWeek = item.DayOfWeek,
                        StartTime = item.StartTime,
                        RoomId = item.RoomId,
                        UserId = instructorId
                    };
                    _context.Classes.Add(newClass);
                }

                _context.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error saving schedule: {ex.Message}" });
            }
        }
    

}
}
