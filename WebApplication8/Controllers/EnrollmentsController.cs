using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication8.Data;
using WebApplication8.Models;

namespace WebApplication8.Controllers
{
    public class EnrollmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public EnrollmentsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager; // initialize it
        }

        // GET: Enrollments
        public async Task<IActionResult> Index(
      string searchEmail,
      int? classId,
      int? courseId,
      string instructorId,
      int? facultyId,
      int? semesterId,
      int? studyProgramId)
        {
            // Base query including all necessary relationships
            var query = _context.Enrollments
                .Include(e => e.User)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Course)
                .Include(e => e.Class)
                    .ThenInclude(c => c.User) // instructor
                .Include(e => e.Class)
                    .ThenInclude(c => c.Semester)
                .ThenInclude(s => s.Classes)
                .AsQueryable();

            // Filter by student email
            if (!string.IsNullOrEmpty(searchEmail))
                query = query.Where(e => e.User != null && e.User.Email.Contains(searchEmail));

            // Filter by class
            if (classId.HasValue && classId.Value != 0)
                query = query.Where(e => e.ClassId == classId.Value);

            // Filter by course
            if (courseId.HasValue && courseId.Value != 0)
                query = query.Where(e => e.Class != null && e.Class.CourseId == courseId.Value);

            // Filter by instructor
            if (!string.IsNullOrEmpty(instructorId))
                query = query.Where(e => e.Class != null && e.Class.UserId == instructorId);

            // Filter by semester
            if (semesterId.HasValue && semesterId.Value != 0)
                query = query.Where(e => e.Class != null && e.Class.SemesterId == semesterId.Value);

            // Filter by faculty
            if (facultyId.HasValue && facultyId.Value != 0)
                query = query.Where(e => e.Class != null
                    && e.Class.Course != null
                    && e.Class.Course.ProgramCourses.Any(pc => pc.StudyProgram.FacultyId == facultyId.Value));

            // Filter by study program
            if (studyProgramId.HasValue && studyProgramId.Value != 0)
                query = query.Where(e => e.Class != null
                    && e.Class.Course != null
                    && e.Class.Course.ProgramCourses.Any(pc => pc.StudyProgramId == studyProgramId.Value));

            // Dropdowns
            ViewBag.Classes = new SelectList(await _context.Classes
                .Include(c => c.Course)
                .Select(c => new { c.ClassId, Name = c.Course.Name + " (ID:" + c.ClassId + ")" })
                .ToListAsync(), "ClassId", "Name", classId);

            ViewBag.Courses = new SelectList(await _context.Courses.ToListAsync(), "CourseId", "Name", courseId);

            ViewBag.Instructors = new SelectList(await _userManager.GetUsersInRoleAsync("Instructor"), "Id", "Email", instructorId);

            ViewBag.Faculties = new SelectList(await _context.Faculties.ToListAsync(), "FacultyId", "Name", facultyId);

            ViewBag.Semesters = new SelectList(await _context.Semesters.ToListAsync(), "SemesterId", "Name", semesterId);

            ViewBag.StudyPrograms = new SelectList(await _context.StudyPrograms.ToListAsync(), "StudyProgramId", "Name", studyProgramId);

            ViewBag.SearchEmail = searchEmail;

            return View(await query.ToListAsync());
        }

        // GET: Enrollments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var enrollment = await _context.Enrollments
                .Include(e => e.Class)
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (enrollment == null)
            {
                return NotFound();
            }

            return View(enrollment);
        }

        // GET: Enrollments/Create
        public IActionResult Create()
        {
            ViewData["ClassId"] = new SelectList(_context.Classes, "ClassId", "ClassId");
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: Enrollments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UserId,ClassId,EnrollmentDate")] Enrollment enrollment)
        {
            if (ModelState.IsValid)
            {
                _context.Add(enrollment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClassId"] = new SelectList(_context.Classes, "ClassId", "ClassId", enrollment.ClassId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", enrollment.UserId);
            return View(enrollment);
        }

        // GET: Enrollments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment == null)
            {
                return NotFound();
            }
            ViewData["ClassId"] = new SelectList(_context.Classes, "ClassId", "ClassId", enrollment.ClassId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", enrollment.UserId);
            return View(enrollment);
        }

        // POST: Enrollments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,ClassId,EnrollmentDate")] Enrollment enrollment)
        {
            if (id != enrollment.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(enrollment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EnrollmentExists(enrollment.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClassId"] = new SelectList(_context.Classes, "ClassId", "ClassId", enrollment.ClassId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", enrollment.UserId);
            return View(enrollment);
        }

        // GET: Enrollments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var enrollment = await _context.Enrollments
                .Include(e => e.Class)
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (enrollment == null)
            {
                return NotFound();
            }

            return View(enrollment);
        }

        // POST: Enrollments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment != null)
            {
                _context.Enrollments.Remove(enrollment);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EnrollmentExists(int id)
        {
            return _context.Enrollments.Any(e => e.Id == id);
        }

        public async Task<IActionResult> Enroll()
        {
            var studentId = _userManager.GetUserId(User);

            // Get student programs
            var studentPrograms = await _context.UserPrograms
                .Where(up => up.UserId == studentId)
                .Select(up => up.StudyProgramId)
                .ToListAsync();

            // Find current semester (based on enrollment or add/drop period)
            var currentSemester = await _context.Semesters
                .FirstOrDefaultAsync(s =>
                    (s.EnrollmentStart <= DateTime.Now && DateTime.Now <= s.EnrollmentEnd) ||
                    (s.AddDropStart <= DateTime.Now && DateTime.Now <= s.AddDropEnd));

            if (currentSemester == null)
            {
                TempData["ErrorMessage"] = "No active enrollment or add/drop period.";
                return RedirectToAction("MyClasses");
            }

            // Calculate total credits already enrolled in this semester
            var totalCredits = await _context.Enrollments
                .Where(e => e.UserId == studentId && e.Class.SemesterId == currentSemester.SemesterId)
                .SumAsync(e => (int?)e.Class.Course.CreditNumber) ?? 0;

            if (totalCredits >= 22)
            {
                TempData["ErrorMessage"] = "You have reached the maximum allowed credit load (22 credits).";
                return RedirectToAction("MyClasses");
            }

            // Get classes for these programs and active enrollment/add-drop
            var classes = await _context.Classes
                .Include(c => c.Course)
                    .ThenInclude(c => c.ProgramCourses)
                        .ThenInclude(pc => pc.CourseType)
                .Include(c => c.Room)
                    .ThenInclude(r => r.Building)
                .Include(c => c.Semester)
                .Include(c => c.User)
                .Include(c => c.Enrollments)
                .Where(c => studentPrograms.Contains(c.Course.ProgramCourses.FirstOrDefault().StudyProgramId) &&
                           (
                               (c.Semester.EnrollmentStart <= DateTime.Now &&
                                DateTime.Now <= c.Semester.EnrollmentEnd)
                               ||
                               (c.Semester.AddDropStart <= DateTime.Now &&
                                DateTime.Now <= c.Semester.AddDropEnd)
                           ))
                .ToListAsync();

            // Get student enrollments and passed courses
            var studentEnrollments = await _context.Enrollments
                .Where(e => e.UserId == studentId)
                .Select(e => e.Class.CourseId)
                .ToListAsync();

            var passedCourses = await _context.StudentGrades
                .Where(g => g.Enrollment.UserId == studentId && g.Score.HasValue && g.Score.Value >= 50)
                .Select(g => g.Enrollment.Class.CourseId)
                .ToListAsync();

            // Build class cards
            var cards = new List<ClassCardViewModel>();
            foreach (var c in classes)
            {
                var alreadyEnrolled = studentEnrollments.Contains(c.CourseId);
                var passedCourse = passedCourses.Contains(c.CourseId);

                // Check prerequisites
                var prereqs = await _context.CoursePrerequisites
                    .Where(cp => cp.CourseId == c.CourseId)
                    .Select(cp => cp.PrerequisiteId)
                    .ToListAsync();

                bool prereqNotSatisfied = false;
                string prereqCourseName = null;
                foreach (var prereqId in prereqs)
                {
                    bool passed = await _context.StudentGrades
                        .Where(g => g.Enrollment.UserId == studentId && g.Enrollment.Class.CourseId == prereqId)
                        .AnyAsync(g => g.Score.HasValue && g.Score.Value >= 60);

                    if (!passed)
                    {
                        prereqNotSatisfied = true;
                        var prereqCourse = await _context.Courses.FindAsync(prereqId);
                        prereqCourseName = prereqCourse?.Name;
                        break;
                    }
                }

                cards.Add(new ClassCardViewModel
                {
                    ClassId = c.ClassId,
                    CourseName = c.Course.Name,
                    CourseTypeName = c.Course.ProgramCourses.FirstOrDefault()?.CourseType?.Name ?? "",
                    InstructorUserName = c.User?.UserName ?? "",
                    RoomName = c.Room?.Name ?? "",
                    BuildingName = c.Room?.Building?.Name ?? "",
                    DayOfWeek = c.DayOfWeek,
                    StartTime = c.StartTime,
                    AvailableSeats = c.Room.Capacity - c.Enrollments.Count,
                    AlreadyEnrolled = alreadyEnrolled,
                    PassedCourse = passedCourse,
                    PrerequisiteNotSatisfied = prereqNotSatisfied,
                    PrerequisiteCourseName = prereqCourseName
                });
            }

            // Distinct course types for filter
            var courseTypes = cards
                .Select(c => c.CourseTypeName)
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct()
                .ToList();

            var model = new EnrollViewModelClasses
            {
                Classes = cards,
                CourseTypes = courseTypes
            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrollStudent(int classId)
        {
            var student = await _userManager.GetUserAsync(User);
            if (student == null)
                return Challenge(); // Not logged in

            var result = await EnrollStudentAsync(student.Id, classId);

            if (result != "Enrollment successful.")
            {
                TempData["ErrorMessage"] = result;
            }
            else
            {
                TempData["SuccessMessage"] = result;
            }

            return RedirectToAction(nameof(Enroll)); // reload the enroll view
        }

        // POST: Enrollments/Enroll
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll(string userId, int classId)
        {
            var result = await EnrollStudentAsync(userId, classId);

            if (result != "Enrollment successful.")
            {
                TempData["ErrorMessage"] = result;
                return RedirectToAction(nameof(Index));
            }


            TempData["SuccessMessage"] = result;
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> EnrollStudentAsync(string studentId, int classId)
        {
            var student = await _context.Users.FindAsync(studentId);
            if (student == null) return "Student not found.";

            var targetClass = await _context.Classes
                .Include(c => c.Room)
                    .ThenInclude(r => r.Building)
                .Include(c => c.Course)
                .Include(c => c.Semester)
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.ClassId == classId);

            if (targetClass == null) return "Class not found.";

            var now = DateTime.UtcNow;

            // 1️⃣ Enrollment OR Add/Drop period check
            if (!((targetClass.Semester.EnrollmentStart <= now && now <= targetClass.Semester.EnrollmentEnd) ||
                  (targetClass.Semester.AddDropStart <= now && now <= targetClass.Semester.AddDropEnd)))
            {
                return "Enrollment is not open for this semester.";
            }

            // 2️⃣ Same campus check
            var studentCampusIds = await _context.UserCampuses
                .Where(uc => uc.UserId == studentId)
                .Select(uc => uc.CampusId)
                .ToListAsync();

            var classCampusId = targetClass.Room?.Building?.CampusId;
            if (classCampusId == null || !studentCampusIds.Contains(classCampusId.Value))
                return "You cannot enroll in a class outside your campus.";

            // 3️⃣ Capacity check
            if (targetClass.Enrollments.Count >= targetClass.Room.Capacity)
                return "Class is full. Cannot enroll.";

            // 4️⃣ Already enrolled in same course
            bool alreadyEnrolledSameCourse = await _context.Enrollments
                .AnyAsync(e => e.UserId == studentId && e.Class.CourseId == targetClass.CourseId);
            if (alreadyEnrolledSameCourse)
                return "You are already enrolled in this course.";

            // 5️⃣ Already passed the course
            bool passed = await _context.StudentGrades
                .Where(g => g.Enrollment.UserId == studentId && g.Enrollment.Class.CourseId == targetClass.CourseId)
                .AnyAsync(g => g.Score.HasValue && g.Score.Value >= 60);
            if (passed)
                return "You have already passed this course.";

            // 6️⃣ Max credits check (22 per semester)
            var totalCredits = await _context.Enrollments
                .Where(e => e.UserId == studentId && e.Class.SemesterId == targetClass.SemesterId)
                .SumAsync(e => (int?)e.Class.Course.CreditNumber) ?? 0;

            if (totalCredits + targetClass.Course.CreditNumber >=22)
                return "You cannot enroll because it exceeds the maximum allowed credits (22).";

            // 7️⃣ Time conflict check (use target class semester!)
            var studentEnrollments = await _context.Enrollments
                .Where(e => e.UserId == studentId && e.Class.SemesterId == targetClass.SemesterId)
                .Include(e => e.Class)
                    .ThenInclude(cl => cl.Course)
                .ToListAsync();

            var targetStart = targetClass.StartTime;
            var targetEnd = targetStart + TimeSpan.FromHours(targetClass.Course.CreditNumber);

            foreach (var e in studentEnrollments)
            {
                if (e.Class.DayOfWeek == targetClass.DayOfWeek)
                {
                    var existingStart = e.Class.StartTime;
                    var existingEnd = existingStart + TimeSpan.FromHours(e.Class.Course.CreditNumber);

                    if (!(targetEnd <= existingStart || targetStart >= existingEnd))
                    {
                        return $"Time conflict with another class (Course: {e.Class.Course.Name}).";
                    }
                }
            }

            // 8️⃣ Prerequisite check
            var prerequisites = await _context.CoursePrerequisites
                .Where(cp => cp.CourseId == targetClass.CourseId)
                .Select(cp => cp.PrerequisiteId)
                .ToListAsync();

            foreach (var prereqId in prerequisites)
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

            // ✅ All checks passed → enroll
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
}
