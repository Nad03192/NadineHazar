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
        public async Task<IActionResult> Index()
        {
            IQueryable<StudentGrade> query = _context.StudentGrades
                .Include(s => s.Enrollment)
                    .ThenInclude(e => e.Class)
                        .ThenInclude(c => c.Course)
                .Include(s => s.Enrollment)
                    .ThenInclude(e => e.User)
                .Include(s => s.GradeDefinition);

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

            return View(await query.ToListAsync());
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

            var enrollments = _context.Enrollments.Include(e => e.Class).ThenInclude(c => c.Course).ToList();
            if (IsInstructor())
                enrollments = enrollments.Where(e => e.Class.UserId == GetUserId()).ToList();

            ViewData["EnrollmentId"] = new SelectList(enrollments, "Id", "Id", studentGrade.EnrollmentId);

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

            var existingGrade = await _context.StudentGrades
                .Include(s => s.Enrollment)
                    .ThenInclude(e => e.Class)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (existingGrade == null) return NotFound();
            if (IsInstructor() && existingGrade.Enrollment.Class.UserId != GetUserId()) return Forbid();

            // ✅ Check duplicate (exclude current record)
            bool duplicateExists = await _context.StudentGrades
                .AnyAsync(sg => sg.EnrollmentId == studentGrade.EnrollmentId
                             && sg.GradeDefinitionId == studentGrade.GradeDefinitionId
                             && sg.Id != studentGrade.Id);

            if (duplicateExists)
            {
                ModelState.AddModelError("", "A grade for this enrollment and grade definition already exists.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(studentGrade);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.StudentGrades.Any(e => e.Id == studentGrade.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            var enrollments = _context.Enrollments.Include(e => e.Class).ThenInclude(c => c.Course).ToList();
            if (IsInstructor())
                enrollments = enrollments.Where(e => e.Class.UserId == GetUserId()).ToList();

            ViewData["EnrollmentId"] = new SelectList(enrollments, "Id", "Id", studentGrade.EnrollmentId);

            ViewData["GradeDefinitionId"] = new SelectList(
                _context.GradeDefinitions.Where(g => g.CourseId == existingGrade.Enrollment.Class.CourseId),
                "Id", "Name", studentGrade.GradeDefinitionId);

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
