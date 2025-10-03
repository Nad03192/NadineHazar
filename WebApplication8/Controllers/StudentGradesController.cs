using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication8.Data;
using WebApplication8.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApplication8.Controllers
{
    public class StudentGradesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public StudentGradesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string GetUserId() => _userManager.GetUserId(User);
        private bool IsAdmin() => User.IsInRole("Admin");
        private bool IsInstructor() => User.IsInRole("Instructor");
        private bool IsStudent() => User.IsInRole("Student");

        // GET: StudentGrades
        // GET: StudentGrades
        // GET: StudentGrades
        public async Task<IActionResult> Index(
            int? semesterId,
            int? courseId,
            int? gradeDefId,
            string search,
            string instructorId)
        {
            // Base query including necessary relations
            IQueryable<StudentGrade> query = _context.StudentGrades
                .Include(s => s.Enrollment)
                    .ThenInclude(e => e.Class)
                        .ThenInclude(c => c.Course)
                .Include(s => s.Enrollment)
                    .ThenInclude(e => e.Class)
                        .ThenInclude(c => c.Semester)
                .Include(s => s.Enrollment)
                    .ThenInclude(e => e.User)
                .Include(s => s.GradeDefinition);

            // Restrict by role
            if (IsInstructor())
            {
                var userId = GetUserId();
                query = query.Where(s => s.Enrollment.Class.UserId == userId);
            }
            else if (IsStudent())
            {
                var userId = GetUserId();
                query = query.Where(s => s.Enrollment.UserId == userId);
            }

            // Apply filters
            if (semesterId.HasValue)
                query = query.Where(s => s.Enrollment.Class.SemesterId == semesterId.Value);

            if (courseId.HasValue)
                query = query.Where(s => s.Enrollment.Class.CourseId == courseId.Value);

            if (gradeDefId.HasValue)
                query = query.Where(s => s.GradeDefinitionId == gradeDefId.Value);

            if (IsAdmin() && !string.IsNullOrEmpty(instructorId))
                query = query.Where(s => s.Enrollment.Class.UserId == instructorId);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(s => s.Enrollment.User != null &&
                                         s.Enrollment.User.Email.Contains(search));

            // Load dropdowns
            var semesters = await _context.Semesters.ToListAsync();
            var courses = await _context.Courses.ToListAsync();
            var gradeDefs = await _context.GradeDefinitions.ToListAsync();

            ViewBag.Semesters = new SelectList(semesters, "SemesterId", "Name", semesterId);
            ViewBag.Courses = new SelectList(courses, "CourseId", "Name", courseId);
            ViewBag.GradeDefinitions = new SelectList(gradeDefs, "Id", "Name", gradeDefId);

            ViewBag.Search = search;

            if (IsAdmin())
            {
                var users = await _userManager.Users.ToListAsync();
                var instructors = new List<IdentityUser>();
                foreach (var user in users)
                {
                    if (await _userManager.IsInRoleAsync(user, "Instructor"))
                        instructors.Add(user);
                }
                ViewBag.Instructors = new SelectList(instructors, "Id", "Email", instructorId);
            }

            var result = await query.ToListAsync();
            return View(result);
        }



        // GET: StudentGrades/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var studentGrade = await _context.StudentGrades
                .Include(s => s.Enrollment)
                    .ThenInclude(e => e.Class)
                        .ThenInclude(c => c.Course)
                .Include(s => s.Enrollment)
                    .ThenInclude(e => e.User)
                .Include(s => s.GradeDefinition)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (studentGrade == null) return NotFound();

            if (IsInstructor() && studentGrade.Enrollment.Class.UserId != GetUserId()) return Forbid();
            if (IsStudent() && studentGrade.Enrollment.UserId != GetUserId()) return Forbid();

            return View(studentGrade);
        }

        // GET: StudentGrades/Create
        public IActionResult Create()
        {
            if (IsStudent()) return Forbid();

            var enrollments = _context.Enrollments
                .Include(e => e.Class)
                    .ThenInclude(c => c.Course)
                .ToList();

            if (IsInstructor())
            {
                var userId = GetUserId();
                enrollments = enrollments.Where(e => e.Class.UserId == userId).ToList();
            }

            ViewData["EnrollmentId"] = new SelectList(enrollments, "Id", "Id");

            // Populate initial grade definitions for first enrollment
            if (enrollments.Any())
            {
                var courseId = enrollments.First().Class.CourseId;
                var gradeDefs = _context.GradeDefinitions.Where(g => g.CourseId == courseId).ToList();
                ViewData["GradeDefinitionId"] = new SelectList(gradeDefs, "Id", "Name");
            }
            else
            {
                ViewData["GradeDefinitionId"] = new SelectList(Enumerable.Empty<GradeDefinition>(), "Id", "Name");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EnrollmentId,GradeDefinitionId,Score")] StudentGrade studentGrade)
        {
            if (IsStudent()) return Forbid();

            // ✅ Check duplicate
            bool duplicateExists = await _context.StudentGrades
                .AnyAsync(sg => sg.EnrollmentId == studentGrade.EnrollmentId
                             && sg.GradeDefinitionId == studentGrade.GradeDefinitionId);

            if (duplicateExists)
            {
                ModelState.AddModelError("", "A grade for this enrollment and grade definition already exists.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(studentGrade);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Re-populate dropdowns
            var enrollments = _context.Enrollments.Include(e => e.Class).ThenInclude(c => c.Course).ToList();
            if (IsInstructor())
                enrollments = enrollments.Where(e => e.Class.UserId == GetUserId()).ToList();

            ViewData["EnrollmentId"] = new SelectList(enrollments, "Id", "Id", studentGrade.EnrollmentId);

            var courseId = _context.Enrollments.Include(e => e.Class)
                .FirstOrDefault(e => e.Id == studentGrade.EnrollmentId)?.Class.CourseId;

            var gradeDefs = _context.GradeDefinitions.Where(g => g.CourseId == courseId).ToList();
            ViewData["GradeDefinitionId"] = new SelectList(gradeDefs, "Id", "Name", studentGrade.GradeDefinitionId);

            return View(studentGrade);
        }


        // GET: StudentGrades/Edit/5
        // GET: StudentGrades/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            if (IsStudent()) return Forbid();

            var studentGrade = await _context.StudentGrades
                .Include(s => s.Enrollment)
                    .ThenInclude(e => e.Class)
                        .ThenInclude(c => c.Course)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (studentGrade == null) return NotFound();

            if (IsInstructor() && studentGrade.Enrollment.Class.UserId != GetUserId()) return Forbid();

            // Prepare Enrollment dropdown with meaningful text
            var enrollments = _context.Enrollments
                .Include(e => e.Class)
                    .ThenInclude(c => c.Course)
                .Include(e => e.User)
                .ToList();

            if (IsInstructor())
                enrollments = enrollments.Where(e => e.Class.UserId == GetUserId()).ToList();

            ViewData["EnrollmentId"] = new SelectList(
                enrollments.Select(e => new { e.Id, Text = $"{e.Class.Course.Name} - {e.User.Email}" }),
                "Id", "Text", studentGrade.EnrollmentId
            );

            // Prepare GradeDefinition dropdown based on the enrollment's course
            var gradeDefs = _context.GradeDefinitions
                .Where(g => g.CourseId == studentGrade.Enrollment.Class.CourseId)
                .ToList();

            ViewData["GradeDefinitionId"] = new SelectList(gradeDefs, "Id", "Name", studentGrade.GradeDefinitionId);

            return View(studentGrade);
        }

        // POST: StudentGrades/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,EnrollmentId,GradeDefinitionId,Score")] StudentGrade studentGrade)
        {
            if (id != studentGrade.Id) return NotFound();
            if (IsStudent()) return Forbid();

            // Load existing grade
            var existingGrade = await _context.StudentGrades
                .Include(s => s.Enrollment)
                    .ThenInclude(e => e.Class)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (existingGrade == null) return NotFound();
            if (IsInstructor() && existingGrade.Enrollment.Class.UserId != GetUserId()) return Forbid();

            // Check for duplicate (excluding current record)
            bool duplicateExists = await _context.StudentGrades
                .AnyAsync(sg => sg.EnrollmentId == studentGrade.EnrollmentId
                             && sg.GradeDefinitionId == studentGrade.GradeDefinitionId
                             && sg.Id != studentGrade.Id);

            if (duplicateExists)
                ModelState.AddModelError("", "A grade for this enrollment and grade definition already exists.");

            if (ModelState.IsValid)
            {
                // Update only the fields we allow
                existingGrade.EnrollmentId = studentGrade.EnrollmentId;
                existingGrade.GradeDefinitionId = studentGrade.GradeDefinitionId;
                existingGrade.Score = studentGrade.Score;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.StudentGrades.Any(e => e.Id == studentGrade.Id))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction(nameof(Index));
            }

            // Repopulate dropdowns if ModelState is invalid
            var enrollments = _context.Enrollments
                .Include(e => e.Class)
                    .ThenInclude(c => c.Course)
                .Include(e => e.User)
                .ToList();

            if (IsInstructor())
                enrollments = enrollments.Where(e => e.Class.UserId == GetUserId()).ToList();

            ViewData["EnrollmentId"] = new SelectList(
                enrollments.Select(e => new { e.Id, Text = $"{e.Class.Course.Name} - {e.User.Email}" }),
                "Id", "Text", studentGrade.EnrollmentId
            );

            // GradeDefinition based on selected enrollment
            var selectedEnrollment = await _context.Enrollments
                .Include(e => e.Class)
                .FirstOrDefaultAsync(e => e.Id == studentGrade.EnrollmentId);

            var gradeDefs = _context.GradeDefinitions
                .Where(g => g.CourseId == selectedEnrollment.Class.CourseId)
                .ToList();

            ViewData["GradeDefinitionId"] = new SelectList(gradeDefs, "Id", "Name", studentGrade.GradeDefinitionId);

            return View(studentGrade);
        }

        // GET: StudentGrades/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            if (IsStudent()) return Forbid();

            var studentGrade = await _context.StudentGrades
                .Include(s => s.Enrollment)
                    .ThenInclude(e => e.Class)
                .Include(s => s.GradeDefinition)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (studentGrade == null) return NotFound();
            if (IsInstructor() && studentGrade.Enrollment.Class.UserId != GetUserId()) return Forbid();

            return View(studentGrade);
        }

        // POST: StudentGrades/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var studentGrade = await _context.StudentGrades
                .Include(sg => sg.Enrollment)
                .ThenInclude(e => e.Class)
                .FirstOrDefaultAsync(sg => sg.Id == id);

            if (studentGrade == null)
            {
                return NotFound();
            }

            if (IsInstructor() && studentGrade.Enrollment.Class.UserId != GetUserId())
            {
                return Forbid();
            }

            _context.StudentGrades.Remove(studentGrade);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        private bool StudentGradeExists(int id) => _context.StudentGrades.Any(e => e.Id == id);

        // AJAX: Get grade definitions for selected enrollment
        public IActionResult GetGradeDefinitions(int enrollmentId)
        {
            var enrollment = _context.Enrollments
                .Include(e => e.Class)
                .FirstOrDefault(e => e.Id == enrollmentId);

            if (enrollment == null) return Json(new List<object>());

            var gradeDefs = _context.GradeDefinitions
                .Where(g => g.CourseId == enrollment.Class.CourseId)
                .Select(g => new { g.Id, g.Name })
                .ToList();

            return Json(gradeDefs);
        }
    }
}
