using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public UserProgramsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: UserPrograms
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.UserPrograms.Include(u => u.StudyProgram).Include(u => u.User);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: UserPrograms/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userProgram = await _context.UserPrograms
                .Include(u => u.StudyProgram)
                .Include(u => u.User)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (userProgram == null)
            {
                return NotFound();
            }

            return View(userProgram);
        }

        // GET: UserPrograms/Create
        public IActionResult Create()
        {
            ViewData["StudyProgramId"] = new SelectList(_context.StudyPrograms, "StudyProgramId", "Name");
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: UserPrograms/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,StudyProgramId")] UserProgram userProgram)
        {
            if (ModelState.IsValid)
            {
                _context.Add(userProgram);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["StudyProgramId"] = new SelectList(_context.StudyPrograms, "StudyProgramId", "Name", userProgram.StudyProgramId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", userProgram.UserId);
            return View(userProgram);
        }

        // GET: UserPrograms/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userProgram = await _context.UserPrograms.FindAsync(id);
            if (userProgram == null)
            {
                return NotFound();
            }
            ViewData["StudyProgramId"] = new SelectList(_context.StudyPrograms, "StudyProgramId", "Name", userProgram.StudyProgramId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", userProgram.UserId);
            return View(userProgram);
        }

        // POST: UserPrograms/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("UserId,StudyProgramId")] UserProgram userProgram)
        {
            if (id != userProgram.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(userProgram);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserProgramExists(userProgram.UserId))
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
            ViewData["StudyProgramId"] = new SelectList(_context.StudyPrograms, "StudyProgramId", "Name", userProgram.StudyProgramId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", userProgram.UserId);
            return View(userProgram);
        }

        // GET: UserPrograms/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userProgram = await _context.UserPrograms
                .Include(u => u.StudyProgram)
                .Include(u => u.User)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (userProgram == null)
            {
                return NotFound();
            }

            return View(userProgram);
        }

        // POST: UserPrograms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var userProgram = await _context.UserPrograms.FindAsync(id);
            if (userProgram != null)
            {
                _context.UserPrograms.Remove(userProgram);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserProgramExists(string id)
        {
            return _context.UserPrograms.Any(e => e.UserId == id);
        }
    }
}
