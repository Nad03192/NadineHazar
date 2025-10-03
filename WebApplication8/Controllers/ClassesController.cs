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
        public async Task<IActionResult> Index(
        int? semesterId,
        string? instructorId,
        int? facultyId,
        int? programId,
        int? courseTypeId,
        int? campusId
    )
        {
            var classes = _context.Classes
                .Include(c => c.Course)
                    .ThenInclude(c => c.ProgramCourses)
                        .ThenInclude(pc => pc.StudyProgram)
                            .ThenInclude(sp => sp.Faculty)
                .Include(c => c.Course)
                    .ThenInclude(c => c.ProgramCourses)
                        .ThenInclude(pc => pc.CourseType)
                .Include(c => c.Room)
                    .ThenInclude(r => r.Building)
                        .ThenInclude(b => b.Campus)
                .Include(c => c.Semester)
                .Include(c => c.User)
                .AsQueryable();

            // Filtering — skip filters if values are null, empty, or default (0)
            if (semesterId.HasValue && semesterId.Value != 0)
                classes = classes.Where(c => c.SemesterId == semesterId.Value);

            if (!string.IsNullOrWhiteSpace(instructorId))
                classes = classes.Where(c => c.UserId == instructorId);

            if (facultyId.HasValue && facultyId.Value != 0)
                classes = classes.Where(c => c.Course.ProgramCourses
                    .Any(pc => pc.StudyProgram.FacultyId == facultyId.Value));

            if (programId.HasValue && programId.Value != 0)
                classes = classes.Where(c => c.Course.ProgramCourses
                    .Any(pc => pc.StudyProgramId == programId.Value));

            if (courseTypeId.HasValue && courseTypeId.Value != 0)
                classes = classes.Where(c => c.Course.ProgramCourses
                    .Any(pc => pc.CourseTypeId == courseTypeId.Value));

            if (campusId.HasValue && campusId.Value != 0)
                classes = classes.Where(c => c.Room.Building.CampusId == campusId.Value);

            // Dropdown lists
            ViewBag.Semesters = new SelectList(_context.Semesters, "SemesterId", "Name", semesterId);
            ViewBag.Instructors = new SelectList(_context.Users, "Id", "Email", instructorId);
            ViewBag.Faculties = new SelectList(_context.Faculties, "FacultyId", "Name", facultyId);
            ViewBag.Programs = new SelectList(_context.StudyPrograms, "StudyProgramId", "Name", programId);
            ViewBag.CourseTypes = new SelectList(_context.CourseTypes, "CourseTypeId", "Name", courseTypeId);
            ViewBag.Campuses = new SelectList(_context.Campuses, "CampusId", "Name", campusId);

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

        // GET: Class/Create
        public IActionResult Create()
        {
            // Choose semester with the nearest SubmitClassesEnd in the future
            var semester = _context.Semesters
                .Where(s => s.SubmitClassesEnd >= DateTime.Now)
                .OrderBy(s => s.SubmitClassesEnd)
                .FirstOrDefault();

            ViewData["SemesterId"] = semester?.SemesterId ?? 0;

            // Courses dropdown
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name");

            // Rooms filtered for the campus of selected semester (you can customize as needed)
            ViewData["RoomId"] = new SelectList(new List<Room>(), "RoomId", "Name");

            // Instructors dropdown
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName");

            return View();
        }


        // POST: Class/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClassId,UserId,CourseId,RoomId,StartTime,DayOfWeek")] Class @class)
        {
            var course = await _context.Courses.FindAsync(@class.CourseId);
            if (course == null)
            {
                ModelState.AddModelError("CourseId", "Invalid course.");
            }

            // Auto-assign semester
            var semester = await _context.Semesters
                .Where(s => s.SubmitClassesEnd >= DateTime.Now)
                .OrderBy(s => s.SubmitClassesEnd)
                .FirstOrDefaultAsync();

            if (semester == null)
            {
                ModelState.AddModelError("", "No semester available to assign this class.");
            }
            else
            {
                @class.SemesterId = semester.SemesterId;
            }

            // Calculate class end time
            var classEndTime = @class.StartTime + TimeSpan.FromHours(course.CreditNumber);

            // Check instructor scheduling conflicts
            if (await HasSchedulingConflict(@class.UserId, @class.DayOfWeek, @class.StartTime, classEndTime))
            {
                ModelState.AddModelError("UserId", "This instructor has a conflicting class at the selected time.");
            }
            // Fetch relevant classes first (without TimeSpan calculation in SQL)
            var roomClasses = _context.Classes
                .Include(c => c.Course)
                .Where(c => c.RoomId == @class.RoomId &&
                            c.SemesterId == @class.SemesterId &&
                            c.DayOfWeek == @class.DayOfWeek &&
                            c.Course != null)
                .AsEnumerable() // switch to in-memory evaluation
                .ToList();      // now this is a List<Class>, not a Task

            // Check overlap in memory
            bool roomConflict = roomClasses.Any(c =>
            {
                var existingStart = c.StartTime;
                var existingEnd = existingStart + TimeSpan.FromHours(c.Course.CreditNumber);
                return @class.StartTime < existingEnd && classEndTime > existingStart;
            });

            if (roomConflict)
            {
                ModelState.AddModelError("RoomId", "This room is already booked for the selected time.");
            }


            if (ModelState.IsValid)
            {
                _context.Add(@class);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Re-populate dropdowns if validation fails
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", @class.CourseId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", @class.UserId);

            var availableRooms = await _context.Rooms
                .Include(r => r.Classes)
                .Where(r => !r.Classes.Any(c => c.SemesterId == semester.SemesterId))
                .ToListAsync();
            ViewData["RoomId"] = new SelectList(availableRooms, "RoomId", "Name", @class.RoomId);

            ViewBag.SemesterName = semester.Name;

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
        public IActionResult GetAvailableRooms(int courseId, string newStartTime, string dayOfWeekStr, string userId)
        {
            if (courseId == 0 || string.IsNullOrEmpty(newStartTime) || string.IsNullOrEmpty(dayOfWeekStr) || string.IsNullOrEmpty(userId))
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

            TimeSpan duration = TimeSpan.FromHours(creditHours);
            TimeSpan newEndTime = startTime + duration;
            if (newEndTime.TotalHours > 24)
                newEndTime = TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(59)).Add(TimeSpan.FromSeconds(59));

            // Get instructor's campus
            var instructorCampusId = _context.UserCampuses
                .Where(uc => uc.UserId == userId)
                .Select(uc => uc.CampusId)
                .FirstOrDefault();

            if (instructorCampusId == 0)
                return Json(new List<SelectListItem>());

            // Filter rooms whose building belongs to the instructor's campus
            var allRooms = _context.Rooms
                .Include(r => r.Building)
                .Where(r => r.Building.CampusId == instructorCampusId)
                .ToList();

            var classesOnSameDay = _context.Classes
                .Include(c => c.Course)
                .Where(c => c.DayOfWeek == dayOfWeek && c.RoomId != 0 && c.Course != null)
                .ToList();

            var unavailableRoomIds = new HashSet<int>();

            foreach (var bookedClass in classesOnSameDay)
            {
                TimeSpan bookedStart = bookedClass.StartTime;
                TimeSpan bookedEnd = bookedStart + TimeSpan.FromHours(bookedClass.Course.CreditNumber);

                if (startTime < bookedEnd && newEndTime > bookedStart)
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
