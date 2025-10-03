using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication8.Data;
using WebApplication8.Models;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication8.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public StudentController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string GetUserId() => _userManager.GetUserId(User);

        // GET: Student Home
        // GET: Student Home
        public async Task<IActionResult> Home()
        {
            var studentId = GetUserId();

            // 1. Get all enrollments of the student
            var enrollments = await _context.Enrollments
                .Include(e => e.Class)
                    .ThenInclude(c => c.Course)
                        .ThenInclude(c => c.GradeDefinitions)
                .Include(e => e.Grades)
                    .ThenInclude(g => g.GradeDefinition)
                .Where(e => e.UserId == studentId)
                .ToListAsync();

            int totalCourses = enrollments.Select(e => e.Class.CourseId).Distinct().Count();
            int passedCourses = 0;

            foreach (var enrollment in enrollments)
            {
                // Calculate weighted score for this enrollment
                double totalScore = 0;
                double totalCoeff = 0;

                foreach (var gradeDef in enrollment.Class.Course.GradeDefinitions)
                {
                    var studentGrade = enrollment.Grades.FirstOrDefault(g => g.GradeDefinitionId == gradeDef.Id);
                    if (studentGrade?.Score != null)
                    {
                        totalScore += studentGrade.Score.Value * gradeDef.Coefficient;
                        totalCoeff += gradeDef.Coefficient;
                    }
                }

                // Normalize weighted score
                double finalScore = totalCoeff > 0 ? totalScore / totalCoeff : 0;

                if (finalScore >= 60)
                    passedCourses++;
            }

            double passedPercentage = totalCourses > 0 ? (double)passedCourses / totalCourses * 100 : 0;

            var model = new StudentHomeViewModel
            {
                PassedPercentage = passedPercentage
            };

            return View(model);
        }


        // In StudentController
        public async Task<IActionResult> MyClasses()
        {
            var studentId = _userManager.GetUserId(User);
            if (studentId == null)
                return Challenge(); // Not logged in

            var now = DateTime.Now;

            // Select semester with StartDate closest to today (upcoming/current)
            var currentSemester = await _context.Semesters
                .Where(s => s.StartDate >= now)
                .OrderBy(s => s.StartDate)
                .FirstOrDefaultAsync();

            if (currentSemester == null)
            {
                TempData["ErrorMessage"] = "No upcoming semester found.";
                return View(new List<MyClassViewModel>());
            }

            // Get student program IDs
            var studentProgramIds = await _context.UserPrograms
                .Where(up => up.UserId == studentId)
                .Select(up => up.StudyProgramId)
                .ToListAsync();

            // Get all classes student is enrolled in for that semester
            var enrollments = await _context.Enrollments
                .Where(e => e.UserId == studentId && e.Class.SemesterId == currentSemester.SemesterId)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Course)
                        .ThenInclude(co => co.ProgramCourses)
                            .ThenInclude(pc => pc.CourseType)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Room)
                        .ThenInclude(r => r.Building)
                .Include(e => e.Class)
                    .ThenInclude(c => c.User)
                .ToListAsync();

            var model = enrollments.Select(e =>
            {
                // Pick the ProgramCourse that matches one of the student's programs
                var programCourse = e.Class.Course.ProgramCourses
                    .FirstOrDefault(pc => studentProgramIds.Contains(pc.StudyProgramId));

                return new MyClassViewModel
                {
                    ClassId = e.ClassId,
                    CourseName = e.Class.Course.Name,
                    CourseTypeName = programCourse?.CourseType?.Name ?? "",
                    InstructorUserName = e.Class.User?.UserName ?? "",
                    RoomName = e.Class.Room?.Name ?? "",
                    BuildingName = e.Class.Room?.Building?.Name ?? "",
                    DayOfWeek = e.Class.DayOfWeek,
                    StartTime = e.Class.StartTime,
                    CreditNumber = e.Class.Course.CreditNumber
                };
            }).ToList();

            ViewBag.SemesterName = currentSemester.Name;

            return View(model);
        }




        // GET: Student/MyGrades
        // GET: /Student/MyGrades
        public async Task<IActionResult> MyGrades()
        {
            var studentId = GetUserId();

            // Get all semesters the student has enrollments in
            var semesters = await _context.Enrollments
                .Where(e => e.UserId == studentId)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Semester)
                .Select(e => e.Class.Semester)
                .Distinct()
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();

            // Get all study programs assigned to the student
            var userProgramIds = await _context.UserPrograms
                .Where(up => up.UserId == studentId)
                .Select(up => up.StudyProgramId)
                .ToListAsync();

            // Get course types for the student's study programs
            var courseTypes = await _context.ProgramCourses
                .Include(pc => pc.CourseType)
                .Where(pc => userProgramIds.Contains(pc.StudyProgramId))
                .Select(pc => pc.CourseType!)
                .Distinct()
                .ToListAsync();

            var model = new StudentGradesViewModel
            {
                Semesters = semesters,
                CourseTypes = courseTypes
            };

            return View(model);
        }


        // AJAX: Get grades with optional filters
        // AJAX: Get grades with optional filters
        public async Task<IActionResult> GetGrades(int? semesterId, int? courseTypeId, string search)
        {
            var studentId = GetUserId();

            // Get the study program(s) assigned to the student
            var userProgramIds = await _context.UserPrograms
                .Where(up => up.UserId == studentId)
                .Select(up => up.StudyProgramId)
                .ToListAsync();

            // Load enrollments with all necessary related data
            var enrollments = await _context.Enrollments
                .Where(e => e.UserId == studentId)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Course)
                        .ThenInclude(c => c.ProgramCourses)
                            .ThenInclude(pc => pc.CourseType)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Semester)
                .Include(e => e.Grades)
                    .ThenInclude(g => g.GradeDefinition)
                .ToListAsync();

            // Filter enrollments by student's program courses and optional course type
            enrollments = enrollments
                .Where(e => e.Class.Course.ProgramCourses
                    .Any(pc => userProgramIds.Contains(pc.StudyProgramId) &&
                               (!courseTypeId.HasValue || pc.CourseTypeId == courseTypeId.Value)))
                .ToList();

            // Filter by semester if provided
            if (semesterId.HasValue)
                enrollments = enrollments
                    .Where(e => e.Class.SemesterId == semesterId.Value)
                    .ToList();

            // Filter by course name search
            if (!string.IsNullOrEmpty(search))
                enrollments = enrollments
                    .Where(e => e.Class.Course.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            // Build DTO with final grade calculation
            var result = enrollments
                .GroupBy(e => e.ClassId)
                .Select(g =>
                {
                    var course = g.First().Class.Course;
                    var grades = course.GradeDefinitions.Select(gd =>
                    {
                        var studentGrade = g.First().Grades.FirstOrDefault(gr => gr.GradeDefinitionId == gd.Id);
                        return new GradeItemDto
                        {
                            GradeDefinitionName = gd.Name,
                            Coefficient = gd.Coefficient,
                            Score = studentGrade?.Score
                        };
                    }).ToList();

                    // Calculate final grade
                    double finalGrade = grades.Sum(gd => (gd.Score ?? 0) * gd.Coefficient);
                    string status = finalGrade >= 60 ? "Pass" : "Fail";

                    return new ClassGradesDto
                    {
                        ClassName = course.Name,
                        CourseName = course.Name,
                        Grades = grades,
                        FinalGrade = finalGrade,
                        Status = status
                    };
                })
                .ToList();

            return Json(result);
        }



        // GET: Enroll / Add & Drop
        public async Task<IActionResult> EnrollAddDrop()
        {
            var studentId = GetUserId();

            // Get all classes open for enrollment
            var now = DateTime.UtcNow;
            var openClasses = await _context.Classes
                .Include(c => c.Course)
                .Include(c => c.Semester)
                .Include(c => c.Room)
                    .ThenInclude(r => r.Building)
                .Where(c => c.Semester.EnrollmentStart <= now && now <= c.Semester.EnrollmentEnd)
                .ToListAsync();

            // Get student current enrollments
            var studentEnrollments = await _context.Enrollments
                .Include(e => e.Class)
                    .ThenInclude(c => c.Course)
                .Where(e => e.UserId == studentId)
                .ToListAsync();

            var model = new EnrollAddDropViewModel
            {
                OpenClasses = openClasses,
                StudentEnrollments = studentEnrollments
            };

            return View(model);
        }

        // GET: Student/DropClasses
        // GET: Student/DropClasses
        public async Task<IActionResult> DropClasses()
        {
            var studentId = _userManager.GetUserId(User);
            if (studentId == null)
                return Challenge(); // Not logged in

            var now = DateTime.Now;

            // Select the semester with StartDate closest to today (upcoming/current)
            var currentSemester = await _context.Semesters
                .Where(s => s.StartDate >= now)
                .OrderBy(s => s.StartDate)
                .FirstOrDefaultAsync();

            if (currentSemester == null)
            {
                TempData["ErrorMessage"] = "No upcoming semester found.";
                return View(new List<MyClassViewModel>());
            }

            // Get student program IDs
            var studentProgramIds = await _context.UserPrograms
                .Where(up => up.UserId == studentId)
                .Select(up => up.StudyProgramId)
                .ToListAsync();

            // Get all classes student is enrolled in for that semester
            var enrollments = await _context.Enrollments
                .Where(e => e.UserId == studentId && e.Class.SemesterId == currentSemester.SemesterId)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Course)
                        .ThenInclude(co => co.ProgramCourses)
                            .ThenInclude(pc => pc.CourseType)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Room)
                        .ThenInclude(r => r.Building)
                .Include(e => e.Class)
                    .ThenInclude(c => c.User)
                .ToListAsync();

            // Compute total credits for current semester
            var totalCredits = enrollments.Sum(e => e.Class.Course.CreditNumber);

            // Check if add/drop period is still open
            bool addDropOpen = now <= currentSemester.AddDropEnd;

            var model = enrollments.Select(e =>
            {
                var programCourse = e.Class.Course.ProgramCourses
                    .FirstOrDefault(pc => studentProgramIds.Contains(pc.StudyProgramId));

                return new MyClassViewModel
                {
                    ClassId = e.ClassId,
                    CourseName = e.Class.Course.Name,
                    CourseTypeName = programCourse?.CourseType?.Name ?? "",
                    InstructorUserName = e.Class.User?.UserName ?? "",
                    RoomName = e.Class.Room?.Name ?? "",
                    BuildingName = e.Class.Room?.Building?.Name ?? "",
                    DayOfWeek = e.Class.DayOfWeek,
                    StartTime = e.Class.StartTime,
                    CreditNumber = e.Class.Course.CreditNumber,
                    CanDrop = addDropOpen && totalCredits > 18 // can drop only if both conditions satisfied
                };
            }).ToList();

            ViewBag.SemesterName = currentSemester.Name;
            ViewBag.TotalCredits = totalCredits;
            ViewBag.AddDropOpen = addDropOpen;

            return View(model);
        }


        // POST: Student/DropClass
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DropClass(int classId)
        {
            var studentId = _userManager.GetUserId(User);
            if (studentId == null)
                return Challenge(); // Not logged in

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.ClassId == classId && e.UserId == studentId);

            if (enrollment == null)
            {
                TempData["ErrorMessage"] = "Enrollment not found.";
                return RedirectToAction(nameof(DropClasses));
            }

            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Class dropped successfully.";
            return RedirectToAction(nameof(DropClasses));
        }


        // GET: Student/SwapClasses
        public async Task<IActionResult> SwapClasses()
        {
            var studentId = _userManager.GetUserId(User);
            if (studentId == null)
                return Challenge();

            var now = DateTime.Now;

            // Select semester with StartDate closest to today (upcoming/current)
            var currentSemester = await _context.Semesters
                .Where(s => s.StartDate >= now)
                .OrderBy(s => s.StartDate)
                .FirstOrDefaultAsync();


            if (currentSemester == null || now > currentSemester.AddDropEnd)
            {
                TempData["ErrorMessage"] = "You cannot swap classes. Add/Drop period has ended.";
                return RedirectToAction("MyClasses");
            }

            // Get student enrolled classes for current semester
            var enrollments = await _context.Enrollments
                .Where(e => e.UserId == studentId && e.Class.SemesterId == currentSemester.SemesterId)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Course)
                        .ThenInclude(co => co.ProgramCourses)
                            .ThenInclude(pc => pc.CourseType)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Room)
                        .ThenInclude(r => r.Building)
                .Include(e => e.Class)
                    .ThenInclude(c => c.User)
                .ToListAsync();

            var studentProgramIds = await _context.UserPrograms
                .Where(up => up.UserId == studentId)
                .Select(up => up.StudyProgramId)
                .ToListAsync();

            // Prepare view model
            var model = enrollments.Select(e =>
            {
                var programCourse = e.Class.Course.ProgramCourses
                    .FirstOrDefault(pc => studentProgramIds.Contains(pc.StudyProgramId));

                return new MyClassViewModel
                {
                    ClassId = e.ClassId,
                    CourseName = e.Class.Course.Name,
                    CourseTypeName = programCourse?.CourseType?.Name ?? "",
                    InstructorUserName = e.Class.User?.UserName ?? "",
                    RoomName = e.Class.Room?.Name ?? "",
                    BuildingName = e.Class.Room?.Building?.Name ?? "",
                    DayOfWeek = e.Class.DayOfWeek,
                    StartTime = e.Class.StartTime,
                    CreditNumber = e.Class.Course.CreditNumber
                };
            }).ToList();

            ViewBag.SemesterName = currentSemester.Name;

            return View(model);
        }

        // GET: Student/SwapClass/{classId}
        // GET: Student/SwapClass/{classId}
        public async Task<IActionResult> SwapClass(int classId)
        {
            var studentId = _userManager.GetUserId(User);
            if (studentId == null) return Challenge();

            var now = DateTime.Now;

            // Select semester with StartDate closest to today (upcoming/current)
            var currentSemester = await _context.Semesters
                .Where(s => s.StartDate >= now)
                .OrderBy(s => s.StartDate)
                .FirstOrDefaultAsync();

            if (currentSemester == null || now > currentSemester.AddDropEnd)
            {
                TempData["ErrorMessage"] = "You cannot swap classes. Add/Drop period has ended.";
                return RedirectToAction("MyClasses");
            }

            // Student's enrolled class
            var enrollment = await _context.Enrollments
                .Include(e => e.Class)
                    .ThenInclude(c => c.Course)
                        .ThenInclude(co => co.ProgramCourses)
                            .ThenInclude(pc => pc.CourseType)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Room)
                        .ThenInclude(r => r.Building)
                .Include(e => e.Class)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(e => e.ClassId == classId && e.UserId == studentId && e.Class.SemesterId == currentSemester.SemesterId);

            if (enrollment == null)
            {
                TempData["ErrorMessage"] = "You are not enrolled in this class.";
                return RedirectToAction("MyClasses");
            }

            // All other classes of the same course in current semester
            var potentialClasses = await _context.Classes
                .Include(c => c.Course)
                    .ThenInclude(co => co.ProgramCourses)
                        .ThenInclude(pc => pc.CourseType)
                .Include(c => c.Room)
                    .ThenInclude(r => r.Building)
                .Include(c => c.User)
                .Include(c => c.Enrollments)
                .Where(c => c.CourseId == enrollment.Class.CourseId &&
                            c.SemesterId == currentSemester.SemesterId &&
                            c.ClassId != classId)
                .ToListAsync();

            // All student enrollments in current semester
            var studentEnrollments = await _context.Enrollments
                .Where(e => e.UserId == studentId && e.Class.SemesterId == currentSemester.SemesterId)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Course)
                .ToListAsync();

            // Filter by all rules
            var availableClasses = new List<ClassCardViewModel>();

            foreach (var c in potentialClasses)
            {
                // 1. Capacity
                if (c.Enrollments.Count >= c.Room.Capacity) continue;

                // 2. Already enrolled in same class
                if (studentEnrollments.Any(e => e.ClassId == c.ClassId)) continue;

                // 3. Prerequisites
                var prereqs = await _context.CoursePrerequisites
                    .Where(cp => cp.CourseId == c.CourseId)
                    .Select(cp => cp.PrerequisiteId)
                    .ToListAsync();

                bool prereqNotSatisfied = false;
                foreach (var prereqId in prereqs)
                {
                    var passed = await _context.StudentGrades
                        .Where(g => g.Enrollment.UserId == studentId && g.Enrollment.Class.CourseId == prereqId)
                        .AnyAsync(g => g.Score.HasValue && g.Score.Value >= 60);

                    if (!passed)
                    {
                        prereqNotSatisfied = true;
                        break;
                    }
                }
                if (prereqNotSatisfied) continue;

                // 4. Time conflict
                var newStart = c.StartTime;
                var newEnd = newStart + TimeSpan.FromHours(c.Course.CreditNumber);
                bool conflict = studentEnrollments
                    .Where(e => e.ClassId != classId)
                    .Any(e =>
                    {
                        var existingStart = e.Class.StartTime;
                        var existingEnd = existingStart + TimeSpan.FromHours(e.Class.Course.CreditNumber);
                        return e.Class.DayOfWeek == c.DayOfWeek &&
                               !(newEnd <= existingStart || newStart >= existingEnd);
                    });
                if (conflict) continue;

                // 5. ✅ Credit limit check (22 max after swap)
                var currentCredits = studentEnrollments
                    .Where(e => e.ClassId != classId) // exclude the one being swapped out
                    .Sum(e => e.Class.Course.CreditNumber);

                if (currentCredits + c.Course.CreditNumber > 22)
                    continue;

                // Passed all rules → add as available
                availableClasses.Add(new ClassCardViewModel
                {
                    ClassId = c.ClassId,
                    CourseName = c.Course.Name,
                    CourseTypeName = c.Course.ProgramCourses.FirstOrDefault()?.CourseType?.Name ?? "",
                    InstructorUserName = c.User?.UserName ?? "",
                    RoomName = c.Room?.Name ?? "",
                    BuildingName = c.Room?.Building?.Name ?? "",
                    DayOfWeek = c.DayOfWeek,
                    StartTime = c.StartTime,
                    AvailableSeats = c.Room.Capacity - c.Enrollments.Count
                });
            }

            var swapModel = new SwapClassesViewModel
            {
                CurrentClassId = enrollment.ClassId,
                CurrentCourseName = enrollment.Class.Course.Name,
                CurrentCourseTypeName = enrollment.Class.Course.ProgramCourses.FirstOrDefault()?.CourseType?.Name ?? "",
                CurrentInstructor = enrollment.Class.User?.UserName ?? "",
                CurrentDayTime = $"{enrollment.Class.DayOfWeek} {enrollment.Class.StartTime}",
                AvailableClasses = availableClasses
            };

            return View(swapModel);
        }

        // POST: Student/SwapClass
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SwapClassConfirmed(int currentClassId, int newClassId)
        {
            var studentId = _userManager.GetUserId(User);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Remove current class
                var currentEnrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.ClassId == currentClassId && e.UserId == studentId);
                if (currentEnrollment == null)
                {
                    TempData["ErrorMessage"] = "Original class enrollment not found.";
                    return RedirectToAction("MyClasses");
                }

                _context.Enrollments.Remove(currentEnrollment);
                await _context.SaveChangesAsync();

                // Enroll into new class
                var result = await EnrollStudentAsync(studentId, newClassId);

                if (result != "Enrollment successful.")
                {
                    throw new Exception(result); // will rollback
                }

                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "Class swapped successfully!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Swap failed: {ex.Message}";
            }

            return RedirectToAction("MyClasses");
        }

        private async Task<string> EnrollStudentAsync(string studentId, int classId)
        {
            var student = await _context.Users.FindAsync(studentId);
            if (student == null) return "Student not found.";

            var targetClass = await _context.Classes
                .Include(c => c.Room)
                .Include(c => c.Course)
                .Include(c => c.Semester)
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.ClassId == classId);

            if (targetClass == null) return "Class not found.";

            // 1. Enrollment period check
            var now = DateTime.UtcNow;
            if (!(targetClass.Semester.EnrollmentStart <= now && now <= targetClass.Semester.EnrollmentEnd))
                return "Enrollment is not open for this semester.";

            // 2. Capacity check
            if (targetClass.Enrollments.Count >= targetClass.Room.Capacity)
                return "Class is full.";

            // 3. Already enrolled in same course
            bool alreadyEnrolled = await _context.Enrollments
                .AnyAsync(e => e.UserId == studentId && e.Class.CourseId == targetClass.CourseId);
            if (alreadyEnrolled)
                return "You are already enrolled in this course.";

            // 4. Already passed course
            bool passed = await _context.StudentGrades
                .Where(g => g.Enrollment.UserId == studentId && g.Enrollment.Class.CourseId == targetClass.CourseId)
                .AnyAsync(g => g.Score.HasValue && g.Score.Value >= 50);
            if (passed) return "You have already passed this course.";

            var currentSemesterEnrollments = await _context.Enrollments
          .Where(e => e.UserId == studentId && e.Class.SemesterId == targetClass.SemesterId)
          .Include(e => e.Class)
              .ThenInclude(c => c.Course) // ✅ include Course
          .ToListAsync();


            var targetStart = targetClass.StartTime;
            var targetEnd = targetStart + TimeSpan.FromHours(targetClass.Course.CreditNumber);

            foreach (var e in currentSemesterEnrollments)
            {
                var existingStart = e.Class.StartTime;
                var existingEnd = existingStart + TimeSpan.FromHours(e.Class.Course.CreditNumber);

                if (e.Class.DayOfWeek == targetClass.DayOfWeek &&
                    !(targetEnd <= existingStart || targetStart >= existingEnd))
                {
                    return $"Time conflict with another class ({e.Class.Course.Name}).";
                }
            }

            // 6. Prerequisite check
            var prereqs = await _context.CoursePrerequisites
                .Where(cp => cp.CourseId == targetClass.CourseId)
                .Select(cp => cp.PrerequisiteId)
                .ToListAsync();

            foreach (var prereqId in prereqs)
            {
                bool hasPassedPrereq = await _context.StudentGrades
                    .Where(g => g.Enrollment.UserId == studentId && g.Enrollment.Class.CourseId == prereqId)
                    .AnyAsync(g => g.Score.HasValue && g.Score.Value >= 60);

                if (!hasPassedPrereq)
                {
                    var prereqCourse = await _context.Courses.FindAsync(prereqId);
                    return $"Cannot enroll: prerequisite not satisfied ({prereqCourse?.Name}).";
                }
            }

            // All good → enroll
            var enrollment = new Enrollment
            {
                UserId = studentId,
                ClassId = classId,
                EnrollmentDate = DateTime.UtcNow
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();
            return "Enrollment successful.";
        }


    }


    // ViewModels
    public class StudentHomeViewModel
    {
        public double PassedPercentage { get; set; }
    }

    public class EnrollAddDropViewModel
    {
        public List<Class> OpenClasses { get; set; } = new List<Class>();
        public List<Enrollment> StudentEnrollments { get; set; } = new List<Enrollment>();
    }
}
