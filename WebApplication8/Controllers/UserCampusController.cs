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
    public class UserCampusController : Controller
    {
        private readonly ApplicationDbContext _context;

      
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserCampusController(ApplicationDbContext context, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index(int? campusId, string role, string searchString)
        {
            var query = _context.UserCampuses
                .Include(uc => uc.User)
                .Include(uc => uc.Campus)
                .AsQueryable();

            // Filter by campus
            if (campusId.HasValue)
                query = query.Where(uc => uc.CampusId == campusId);

            // Filter by role
            if (!string.IsNullOrEmpty(role))
            {
                var userIdsInRole = await (from userRole in _context.UserRoles
                                           where userRole.RoleId == role
                                           select userRole.UserId).ToListAsync();

                query = query.Where(uc => userIdsInRole.Contains(uc.UserId));
            }

            // Search by email, phone, or username
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(uc =>
                    uc.User.Email.Contains(searchString) ||
                    uc.User.PhoneNumber.Contains(searchString) ||
                    uc.User.UserName.Contains(searchString));
            }

            // Dropdowns
            ViewBag.CampusList = new SelectList(await _context.Campuses.ToListAsync(), "CampusId", "Name", campusId);
            ViewBag.RoleList = new SelectList(await _roleManager.Roles.ToListAsync(), "Id", "Name", role);

            // Get user roles for display
            var userRoles = await (from ur in _context.UserRoles
                                   join r in _context.Roles on ur.RoleId equals r.Id
                                   select new { ur.UserId, r.Name }).ToListAsync();

            ViewBag.UserRoles = userRoles
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

            ViewBag.SelectedCampus = campusId?.ToString();
            ViewBag.SelectedRole = role;
            ViewBag.SearchString = searchString;

            return View(await query.ToListAsync());
        }


        // GET: UserCampus/Details
        public async Task<IActionResult> Details(string userId, int campusId)
        {
            if (string.IsNullOrEmpty(userId) || campusId == 0)
                return NotFound();

            var userCampus = await _context.UserCampuses
                .Include(uc => uc.Campus)
                .Include(uc => uc.User)
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CampusId == campusId);

            if (userCampus == null)
                return NotFound();

            return View(userCampus);
        }

        // GET: UserCampus/Create
        public IActionResult Create()
        {
            ViewData["CampusId"] = new SelectList(_context.Campuses, "CampusId", "Name");
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,CampusId")] UserCampus userCampus)
        {
            // ✅ Check if this user-campus combination already exists
            bool exists = await _context.UserCampuses
                .AnyAsync(uc => uc.UserId == userCampus.UserId && uc.CampusId == userCampus.CampusId);

            if (exists)
            {
                ModelState.AddModelError("", "This user is already assigned to the selected campus.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(userCampus);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CampusId"] = new SelectList(_context.Campuses, "CampusId", "Name", userCampus.CampusId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", userCampus.UserId);
            return View(userCampus);
        }

        // GET: UserCampus/Edit
        public async Task<IActionResult> Edit(string userId, int campusId)
        {
            if (string.IsNullOrEmpty(userId) || campusId == 0)
                return NotFound();

            var userCampus = await _context.UserCampuses
                .Include(uc => uc.User)
                .Include(uc => uc.Campus)
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CampusId == campusId);

            if (userCampus == null)
                return NotFound();

            ViewData["CampusId"] = new SelectList(_context.Campuses, "CampusId", "Name", userCampus.CampusId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", userCampus.UserId);
            return View(userCampus);
        }

        // POST: UserCampus/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string userId, int campusId, [Bind("UserId,CampusId")] UserCampus userCampus)
        {
            if (userId != userCampus.UserId || campusId != userCampus.CampusId)
                return NotFound();

            // ✅ Check uniqueness excluding the current record
            bool exists = await _context.UserCampuses
                .AnyAsync(uc => uc.UserId == userCampus.UserId && uc.CampusId == userCampus.CampusId);

            if (exists)
            {
                ModelState.AddModelError("", "This user is already assigned to the selected campus.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(userCampus);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserCampusExists(userCampus.UserId, userCampus.CampusId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["CampusId"] = new SelectList(_context.Campuses, "CampusId", "Name", userCampus.CampusId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", userCampus.UserId);
            return View(userCampus);
        }

        // GET: UserCampus/Delete
        public async Task<IActionResult> Delete(string userId, int campusId)
        {
            if (string.IsNullOrEmpty(userId) || campusId == 0)
                return NotFound();

            var userCampus = await _context.UserCampuses
                .Include(uc => uc.Campus)
                .Include(uc => uc.User)
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CampusId == campusId);

            if (userCampus == null)
                return NotFound();

            return View(userCampus);
        }

        // POST: UserCampus/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string userId, int campusId)
        {
            var userCampus = await _context.UserCampuses
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CampusId == campusId);

            if (userCampus != null)
            {
                _context.UserCampuses.Remove(userCampus);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool UserCampusExists(string userId, int campusId)
        {
            return _context.UserCampuses.Any(uc => uc.UserId == userId && uc.CampusId == campusId);
        }
    }
}
