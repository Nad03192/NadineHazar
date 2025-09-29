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
            ViewBag.Search = search; // preserve the search value
            return View(model);
        }



        // GET: ProgramManagers/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var programManager = await _context.ProgramManager
                .Include(p => p.StudyProgram)
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (programManager == null)
            {
                return NotFound();
            }

            return View(programManager);
        }

        // GET: ProgramManagers/Create
        public async Task<IActionResult> Create()
        {
            var instructors = await GetInstructorsAsync();

            ViewData["UserId"] = new SelectList(instructors, "Id", "Email"); // display email
            ViewData["StudyProgramId"] = new SelectList(await _context.StudyPrograms.ToListAsync(), "StudyProgramId", "Name");

            return View();
        }



        // POST: ProgramManagers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,StudyProgramId")] ProgramManager programManager)
        {
            if (ModelState.IsValid)
            {
                _context.Add(programManager);
                await _context.SaveChangesAsync();

                // Assign the user to the "ProgramManager" role
                var user = await _userManager.FindByIdAsync(programManager.UserId);
                if (user != null && !await _userManager.IsInRoleAsync(user, "ProgramManager"))
                {
                    await _userManager.AddToRoleAsync(user, "ProgramManager");
                }

                return RedirectToAction(nameof(Index));
            }

            // Re-populate the dropdowns on validation failure
            var instructors = new List<IdentityUser>();
            foreach (var user in _context.Users)
            {
                if (await _userManager.IsInRoleAsync(user, "Instructor"))
                    instructors.Add(user);
            }

            ViewData["UserId"] = new SelectList(instructors, "Id", "Email", programManager.UserId);
            ViewData["StudyProgramId"] = new SelectList(_context.StudyPrograms, "StudyProgramId", "Name", programManager.StudyProgramId);

            return View(programManager);
        }

        // GET: ProgramManagers/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var programManager = await _context.ProgramManager.FindAsync(id);
            if (programManager == null)
            {
                return NotFound();
            }
            ViewData["StudyProgramId"] = new SelectList(_context.StudyPrograms, "StudyProgramId", "Name", programManager.StudyProgramId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", programManager.UserId);
            return View(programManager);
        }

        // POST: ProgramManagers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("UserId,StudyProgramId")] ProgramManager programManager)
        {
            if (id != programManager.UserId)
            {
                return NotFound();
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
                    if (!ProgramManagerExists(programManager.UserId))
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
            ViewData["StudyProgramId"] = new SelectList(_context.StudyPrograms, "StudyProgramId", "Name", programManager.StudyProgramId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", programManager.UserId);
            return View(programManager);
        }

        // GET: ProgramManagers/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var programManager = await _context.ProgramManager
                .Include(p => p.StudyProgram)
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (programManager == null)
            {
                return NotFound();
            }

            return View(programManager);
        }

        // POST: ProgramManagers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var programManager = await _context.ProgramManager.FindAsync(id);
            if (programManager != null)
            {
                _context.ProgramManager.Remove(programManager);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        // Helper to get all users with Instructor role
        private async Task<List<IdentityUser>> GetInstructorsAsync()
        {
            var instructorRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Instructor");
            if (instructorRole == null) return new List<IdentityUser>();

            var instructors = await _context.UserRoles
                .Where(ur => ur.RoleId == instructorRole.Id)
                .Join(_context.Users,
                      ur => ur.UserId,
                      u => u.Id,
                      (ur, u) => u)
                .ToListAsync();

            return instructors;
        }
        private bool ProgramManagerExists(string id)
        {
            return _context.ProgramManager.Any(e => e.UserId == id);
        }
    }
}
