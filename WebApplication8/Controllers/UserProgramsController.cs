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
    public class UserProgramsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public UserProgramsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        // GET: UserPrograms
        public async Task<IActionResult> Index(string searchEmail, int? studyProgramId, int? facultyId)
        {
            var query = _context.UserPrograms
                .Include(u => u.StudyProgram)
                    .ThenInclude(sp => sp.Faculty)
                .Include(u => u.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchEmail))
                query = query.Where(u => u.User != null && u.User.Email.Contains(searchEmail));

            if (facultyId.HasValue && facultyId.Value != 0)
                query = query.Where(u => u.StudyProgram != null && u.StudyProgram.FacultyId == facultyId.Value);

            if (studyProgramId.HasValue && studyProgramId.Value != 0)
                query = query.Where(u => u.StudyProgramId == studyProgramId.Value);

            // Dropdowns — ensure these are never null
            var faculties = await _context.Faculties.ToListAsync();
            ViewBag.Faculties = new SelectList(faculties, "FacultyId", "Name", facultyId);

            var studyPrograms = await _context.StudyPrograms.ToListAsync();
            ViewBag.StudyPrograms = new SelectList(studyPrograms, "StudyProgramId", "Name", studyProgramId);

            // Use ViewBag for search input
            ViewBag.SearchEmail = searchEmail;

            return View(await query.ToListAsync());
        }



        // GET: UserPrograms/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
                return NotFound();

            var userProgram = await _context.UserPrograms
                .Include(u => u.StudyProgram)
                .Include(u => u.User)
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (userProgram == null)
                return NotFound();

            return View(userProgram);
        }

        // GET: UserPrograms/Create
        public async Task<IActionResult> Create()
        {
            var students = await GetStudentsAsync();
            ViewData["UserId"] = new SelectList(students, "Id", "Email");
            ViewData["StudyProgramId"] = new SelectList(_context.StudyPrograms, "StudyProgramId", "Name");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,StudyProgramId")] UserProgram userProgram)
        {
            // Check if combination already exists
            bool exists = await _context.UserPrograms
                .AnyAsync(up => up.UserId == userProgram.UserId && up.StudyProgramId == userProgram.StudyProgramId);

            if (exists)
            {
                ModelState.AddModelError("", "This student is already assigned to this study program.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(userProgram);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var students = await GetStudentsAsync();
            ViewData["UserId"] = new SelectList(students, "Id", "Email", userProgram.UserId);
            ViewData["StudyProgramId"] = new SelectList(_context.StudyPrograms, "StudyProgramId", "Name", userProgram.StudyProgramId);

            return View(userProgram);
        }



        // GET: UserPrograms/Edit
        public async Task<IActionResult> Edit(string userId, int studyProgramId)
        {
            if (string.IsNullOrEmpty(userId) || studyProgramId == 0)
                return NotFound();

            var userProgram = await _context.UserPrograms
                .Include(up => up.User)
                .Include(up => up.StudyProgram)
                .FirstOrDefaultAsync(up => up.UserId == userId && up.StudyProgramId == studyProgramId);

            if (userProgram == null)
                return NotFound();

            // Populate dropdowns
            var students = await _userManager.GetUsersInRoleAsync("Student");
            ViewBag.Users = new SelectList(students, "Id", "Email", userProgram.UserId);

            var studyPrograms = await _context.StudyPrograms.ToListAsync();
            ViewBag.StudyPrograms = new SelectList(studyPrograms, "StudyProgramId", "Name", userProgram.StudyProgramId);

            return View(userProgram);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string userId, int studyProgramId, UserProgram model)
        {
            if (userId != model.UserId || studyProgramId != model.StudyProgramId)
                return BadRequest();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(model);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.UserPrograms.Any(up => up.UserId == userId && up.StudyProgramId == studyProgramId))
                        return NotFound();
                    else
                        throw;
                }
            }

            // Reload dropdowns if model state invalid
            var allStudents = await _userManager.GetUsersInRoleAsync("Student");
            ViewBag.Users = new SelectList(allStudents, "Id", "Email", model.UserId);

            var allPrograms = await _context.StudyPrograms.ToListAsync();
            ViewBag.StudyPrograms = new SelectList(allPrograms, "StudyProgramId", "Name", model.StudyProgramId);

            return View(model);
        }





        // GET: UserPrograms/Delete
        public async Task<IActionResult> Delete(string userId, int studyProgramId)
        {
            if (userId == null || studyProgramId == 0)
                return NotFound();

            var userProgram = await _context.UserPrograms
                .Include(up => up.User)
                .Include(up => up.StudyProgram)
                .FirstOrDefaultAsync(up => up.UserId == userId && up.StudyProgramId == studyProgramId);

            if (userProgram == null)
                return NotFound();

            return View(userProgram);
        }

        // POST: UserPrograms/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string userId, int studyProgramId)
        {
            var userProgram = await _context.UserPrograms
                .FirstOrDefaultAsync(up => up.UserId == userId && up.StudyProgramId == studyProgramId);

            if (userProgram == null)
                return NotFound();

            _context.UserPrograms.Remove(userProgram);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }



        private async Task<List<IdentityUser>> GetStudentsAsync()
        {
            return (await _userManager.GetUsersInRoleAsync("Student")).ToList();
        }

    }

}

