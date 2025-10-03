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
    public class ProgramCoursesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ProgramCoursesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Helper method to get logged-in user's managed study program
        private async Task<int?> GetManagedStudyProgramId()
        {
            var userId = _userManager.GetUserId(User);
            if (User.IsInRole("Admin"))
                return null; // Admin can access all

            var manager = await _context.ProgramManager
                .FirstOrDefaultAsync(pm => pm.UserId == userId);

            return manager?.StudyProgramId;
        }

        // GET: ProgramCourses
        public async Task<IActionResult> Index(string search, int? courseTypeId)
        {
            var managedProgramId = await GetManagedStudyProgramId();

            var query = _context.ProgramCourses
                .Include(p => p.Course)
                .Include(p => p.CourseType)
                .Include(p => p.StudyProgram)
                .AsQueryable();

            if (managedProgramId.HasValue)
                query = query.Where(pc => pc.StudyProgramId == managedProgramId.Value);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(pc => pc.Course.Name.Contains(search));

            if (courseTypeId.HasValue)
                query = query.Where(pc => pc.CourseTypeId == courseTypeId.Value);

            ViewBag.CourseTypes = new SelectList(_context.CourseTypes, "CourseTypeId", "Name", courseTypeId);

            return View(await query.ToListAsync());
        }

        // GET: ProgramCourses/Details
        public async Task<IActionResult> Details(int studyProgramId, int courseId)
        {
            var programCourse = await _context.ProgramCourses
                .Include(p => p.Course)
                .Include(p => p.CourseType)
                .Include(p => p.StudyProgram)
                .FirstOrDefaultAsync(pc => pc.StudyProgramId == studyProgramId && pc.CourseId == courseId);

            if (programCourse == null) return NotFound();

            var managedProgramId = await GetManagedStudyProgramId();
            if (managedProgramId.HasValue && programCourse.StudyProgramId != managedProgramId.Value)
                return Forbid();

            return View(programCourse);
        }

        // GET: ProgramCourses/Create
        public async Task<IActionResult> Create()
        {
            var managedProgramId = await GetManagedStudyProgramId();

            if (managedProgramId.HasValue)
                ViewData["StudyProgramId"] = new SelectList(
                    _context.StudyPrograms.Where(sp => sp.StudyProgramId == managedProgramId.Value),
                    "StudyProgramId", "Name");
            else
                ViewData["StudyProgramId"] = new SelectList(_context.StudyPrograms, "StudyProgramId", "Name");

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name");
            ViewData["CourseTypeId"] = new SelectList(_context.CourseTypes, "CourseTypeId", "Name");
            return View();
        }

        // POST: ProgramCourses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StudyProgramId,CourseId,CourseTypeId")] ProgramCourse programCourse)
        {
            var managedProgramId = await GetManagedStudyProgramId();
            if (managedProgramId.HasValue && programCourse.StudyProgramId != managedProgramId.Value)
                return Forbid();

            // ✅ Check uniqueness
            bool exists = await _context.ProgramCourses
                .AnyAsync(pc => pc.StudyProgramId == programCourse.StudyProgramId && pc.CourseId == programCourse.CourseId);

            if (exists)
            {
                ModelState.AddModelError("", "This course is already assigned to this study program.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(programCourse);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", programCourse.CourseId);
            ViewData["CourseTypeId"] = new SelectList(_context.CourseTypes, "CourseTypeId", "Name", programCourse.CourseTypeId);
            ViewData["StudyProgramId"] = new SelectList(_context.StudyPrograms, "StudyProgramId", "Name", programCourse.StudyProgramId);
            return View(programCourse);
        }


        // GET: ProgramCourses/Edit
        public async Task<IActionResult> Edit(int studyProgramId, int courseId)
        {
            var programCourse = await _context.ProgramCourses
                .FirstOrDefaultAsync(pc => pc.StudyProgramId == studyProgramId && pc.CourseId == courseId);

            if (programCourse == null) return NotFound();

            var managedProgramId = await GetManagedStudyProgramId();
            if (managedProgramId.HasValue && programCourse.StudyProgramId != managedProgramId.Value)
                return Forbid();

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", programCourse.CourseId);
            ViewData["CourseTypeId"] = new SelectList(_context.CourseTypes, "CourseTypeId", "Name", programCourse.CourseTypeId);
            ViewData["StudyProgramId"] = new SelectList(_context.StudyPrograms, "StudyProgramId", "Name", programCourse.StudyProgramId);
            return View(programCourse);
        }

        // POST: ProgramCourses/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int studyProgramId, int courseId, [Bind("StudyProgramId,CourseId,CourseTypeId")] ProgramCourse programCourse)
        {
            if (studyProgramId != programCourse.StudyProgramId || courseId != programCourse.CourseId)
                return NotFound();

            // ✅ Check uniqueness excluding the current record
            bool exists = await _context.ProgramCourses
                .AnyAsync(pc => pc.StudyProgramId == programCourse.StudyProgramId
                            && pc.CourseId == programCourse.CourseId
                            && (pc.StudyProgramId != studyProgramId || pc.CourseId != courseId));

            if (exists)
            {
                ModelState.AddModelError("", "This course is already assigned to this study program.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(programCourse);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.ProgramCourses.Any(pc => pc.StudyProgramId == studyProgramId && pc.CourseId == courseId))
                        return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Name", programCourse.CourseId);
            ViewData["CourseTypeId"] = new SelectList(_context.CourseTypes, "CourseTypeId", "Name", programCourse.CourseTypeId);
            ViewData["StudyProgramId"] = new SelectList(_context.StudyPrograms, "StudyProgramId", "Name", programCourse.StudyProgramId);
            return View(programCourse);
        }


        // GET: ProgramCourses/Delete
        public async Task<IActionResult> Delete(int studyProgramId, int courseId)
        {
            var programCourse = await _context.ProgramCourses
                .Include(pc => pc.Course)
                .Include(pc => pc.CourseType)
                .Include(pc => pc.StudyProgram)
                .FirstOrDefaultAsync(pc => pc.StudyProgramId == studyProgramId && pc.CourseId == courseId);

            if (programCourse == null) return NotFound();

            var managedProgramId = await GetManagedStudyProgramId();
            if (managedProgramId.HasValue && programCourse.StudyProgramId != managedProgramId.Value)
                return Forbid();

            return View(programCourse);
        }

        // POST: ProgramCourses/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int studyProgramId, int courseId)
        {
            var programCourse = await _context.ProgramCourses
                .FirstOrDefaultAsync(pc => pc.StudyProgramId == studyProgramId && pc.CourseId == courseId);

            var managedProgramId = await GetManagedStudyProgramId();
            if (managedProgramId.HasValue && programCourse.StudyProgramId != managedProgramId.Value)
                return Forbid();

            if (programCourse != null)
            {
                _context.ProgramCourses.Remove(programCourse);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
