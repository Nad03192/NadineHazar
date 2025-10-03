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
    public class ProgramManagersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ProgramManagersController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: ProgramManagers
        public async Task<IActionResult> Index(string search)
        {
            var query = _context.ProgramManager
                                .Include(pm => pm.User)
                                .Include(pm => pm.StudyProgram)
                                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(pm =>
                    (pm.User != null && pm.User.Email.Contains(search)) ||
                    (pm.StudyProgram != null && pm.StudyProgram.Name.Contains(search))
                );
            }

            var model = await query.ToListAsync();
            ViewBag.Search = search;
            return View(model);
        }

        // GET: ProgramManagers/Details
        public async Task<IActionResult> Details(string userId, int studyProgramId)
        {
            if (userId == null)
                return NotFound();

            var programManager = await _context.ProgramManager
                .Include(p => p.User)
                .Include(p => p.StudyProgram)
                .FirstOrDefaultAsync(pm => pm.UserId == userId && pm.StudyProgramId == studyProgramId);

            if (programManager == null)
                return NotFound();

            return View(programManager);
        }

        // GET: ProgramManagers/Create
        public async Task<IActionResult> Create()
        {
           
            var instructors = await GetInstructorsAsync();
            ViewData["UserId"] = new SelectList(instructors, "Id", "Email");

            ViewData["StudyProgramId"] = new SelectList(await _context.StudyPrograms.ToListAsync(), "StudyProgramId", "Name");
            return View();
        }

        // POST: ProgramManagers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,StudyProgramId")] ProgramManager programManager)
        {
            // ✅ Check if this user is already a manager for the same program
            bool exists = await _context.ProgramManager
                .AnyAsync(pm => pm.UserId == programManager.UserId && pm.StudyProgramId == programManager.StudyProgramId);

            if (exists)
            {
                ModelState.AddModelError("", "This user is already assigned as manager for this study program.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(programManager);
                await _context.SaveChangesAsync();

                // Assign ProgramManager role
                var user = await _userManager.FindByIdAsync(programManager.UserId);
                if (user != null && !await _userManager.IsInRoleAsync(user, "ProgramManager"))
                {
                    await _userManager.AddToRoleAsync(user, "ProgramManager");
                }

                return RedirectToAction(nameof(Index));
            }

            // Re-populate dropdowns if validation fails
            var instructors = await GetInstructorsAsync();
            ViewData["UserId"] = new SelectList(instructors, "Id", "Email", programManager.UserId);
            ViewData["StudyProgramId"] = new SelectList(_context.StudyPrograms, "StudyProgramId", "Name", programManager.StudyProgramId);
            return View(programManager);
        }


        // GET: ProgramManagers/Edit
        public async Task<IActionResult> Edit(string userId, int studyProgramId)
        {
            if (userId == null)
                return NotFound();

            var programManager = await _context.ProgramManager
                .FirstOrDefaultAsync(pm => pm.UserId == userId && pm.StudyProgramId == studyProgramId);

            if (programManager == null)
                return NotFound();
            var instructors = await GetInstructorsAsync();
            ViewData["UserId"] = new SelectList(instructors, "Id", "Email");

            ViewData["StudyProgramId"] = new SelectList(_context.StudyPrograms, "StudyProgramId", "Name", programManager.StudyProgramId);
            return View(programManager);
        }

        // POST: ProgramManagers/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string userId, int studyProgramId, [Bind("UserId,StudyProgramId")] ProgramManager programManager)
        {
            if (userId != programManager.UserId || studyProgramId != programManager.StudyProgramId)
                return NotFound();

            // ✅ Check uniqueness excluding the current record (optional if editing primary keys)
            bool exists = await _context.ProgramManager
                .AnyAsync(pm => pm.UserId == programManager.UserId && pm.StudyProgramId == programManager.StudyProgramId);

            if (exists)
            {
                ModelState.AddModelError("", "This user is already assigned as manager for this study program.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(programManager);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProgramManagerExists(programManager.UserId, programManager.StudyProgramId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", programManager.UserId);
            ViewData["StudyProgramId"] = new SelectList(_context.StudyPrograms, "StudyProgramId", "Name", programManager.StudyProgramId);
            return View(programManager);
        }


        // GET: ProgramManagers/Delete
        public async Task<IActionResult> Delete(string userId, int studyProgramId)
        {
            if (userId == null)
                return NotFound();

            var programManager = await _context.ProgramManager
                .Include(p => p.User)
                .Include(p => p.StudyProgram)
                .FirstOrDefaultAsync(pm => pm.UserId == userId && pm.StudyProgramId == studyProgramId);

            if (programManager == null)
                return NotFound();

            return View(programManager);
        }

        // POST: ProgramManagers/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string userId, int studyProgramId)
        {
            var programManager = await _context.ProgramManager
                .FirstOrDefaultAsync(pm => pm.UserId == userId && pm.StudyProgramId == studyProgramId);

            if (programManager != null)
                _context.ProgramManager.Remove(programManager);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Helper: Get users with Instructor role
        private async Task<List<IdentityUser>> GetInstructorsAsync()
        {
            var instructorRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Instructor");
            if (instructorRole == null) return new List<IdentityUser>();

            var instructors = await _context.UserRoles
                .Where(ur => ur.RoleId == instructorRole.Id)
                .Join(_context.Users, ur => ur.UserId, u => u.Id, (ur, u) => u)
                .ToListAsync();

            return instructors;
        }

        private bool ProgramManagerExists(string userId, int studyProgramId)
        {
            return _context.ProgramManager.Any(pm => pm.UserId == userId && pm.StudyProgramId == studyProgramId);
        }
    }
}
