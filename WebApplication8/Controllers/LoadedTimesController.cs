using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using WebApplication8.Data;
using WebApplication8.Models;

namespace WebApplication8.Controllers
{
    public class LoadedTimesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public LoadedTimesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: LoadedTimes
        public async Task<IActionResult> Index(string searchEmail)
        {
            var query = _context.LoadedTimes.Include(l => l.User).AsQueryable();

            if (!string.IsNullOrEmpty(searchEmail))
            {
                query = query.Where(l => l.User.Email.Contains(searchEmail));
            }

            var loadedTimes = await query.ToListAsync();
            ViewBag.SearchEmail = searchEmail; // Keep the search value in the textbox
            return View(loadedTimes);
        }
        // GET: LoadedTimes/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
                return NotFound();

            var loadedTime = await _context.LoadedTimes
                .Include(l => l.User)
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (loadedTime == null)
                return NotFound();

            return View(loadedTime);
        }

        // GET: LoadedTimes/Create
        public IActionResult Create()
        {
            // Get the role id of "Instructor"
            var instructorRoleId = _context.Roles
                .Where(r => r.Name == "Instructor")
                .Select(r => r.Id)
                .FirstOrDefault();

            if (instructorRoleId == null)
            {
                // No Instructor role found, return empty list or handle error
                ViewBag.UserId = new SelectList(Enumerable.Empty<SelectListItem>());
                return View();
            }

            // Users who have the Instructor role
            var instructorUserIds = _context.UserRoles
                .Where(ur => ur.RoleId == instructorRoleId)
                .Select(ur => ur.UserId)
                .ToList();

            // Users who already have LoadedTime
            var usersWithLoadedTime = _context.LoadedTimes.Select(lt => lt.UserId).ToList();

            // Filter users: instructor users who don't have LoadedTime yet
            var availableUsers = _context.Users
                .Where(u => instructorUserIds.Contains(u.Id) && !usersWithLoadedTime.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    Display = u.Email
                })
                .ToList();

            ViewBag.UserId = new SelectList(availableUsers, "Id", "Display");

            return View();
        }

        // POST: LoadedTimes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,HoursPerWeek")] LoadedTime loadedTime)
        {
            if (ModelState.IsValid)
            {
                _context.Add(loadedTime);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Repeat filtering here as well
            var instructorRoleId = _context.Roles
                .Where(r => r.Name == "Instructor")
                .Select(r => r.Id)
                .FirstOrDefault();

            var instructorUserIds = _context.UserRoles
                .Where(ur => ur.RoleId == instructorRoleId)
                .Select(ur => ur.UserId)
                .ToList();

            var usersWithLoadedTime = _context.LoadedTimes.Select(lt => lt.UserId).ToList();

            var availableUsers = _context.Users
                .Where(u => instructorUserIds.Contains(u.Id) && !usersWithLoadedTime.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    Display = u.Email
                })
                .ToList();

            ViewBag.UserId = new SelectList(availableUsers, "Id", "Display", loadedTime.UserId);

            return View(loadedTime);
        }



        // GET: LoadedTimes/Edit/5
        // GET: LoadedTimes/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
                return NotFound();

            // Eager-load the User
            var loadedTime = await _context.LoadedTimes
                .Include(l => l.User)    // <- important
                .FirstOrDefaultAsync(l => l.UserId == id);

            if (loadedTime == null)
                return NotFound();

            return View(loadedTime);
        }

        // POST: LoadedTimes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("UserId,HoursPerWeek")] LoadedTime loadedTime)
        {
            if (id != loadedTime.UserId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(loadedTime);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LoadedTimeExists(loadedTime.UserId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", loadedTime.UserId);
            return View(loadedTime);
        }

        // GET: LoadedTimes/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
                return NotFound();

            var loadedTime = await _context.LoadedTimes
                .Include(l => l.User)
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (loadedTime == null)
                return NotFound();

            return View(loadedTime);
        }

        // POST: LoadedTimes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var loadedTime = await _context.LoadedTimes.FindAsync(id);
            if (loadedTime != null)
            {
                _context.LoadedTimes.Remove(loadedTime);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool LoadedTimeExists(string id)
        {
            return _context.LoadedTimes.Any(e => e.UserId == id);
        }
    }
}
