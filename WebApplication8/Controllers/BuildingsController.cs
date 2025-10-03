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
    public class BuildingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BuildingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Buildings
        public async Task<IActionResult> Index(string searchString, int? campusId)
        {
            ViewBag.CurrentFilter = searchString;
            ViewBag.SelectedCampus = campusId?.ToString();

            var buildings = _context.Buildings
                .Include(b => b.Campus)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                buildings = buildings.Where(b =>
                    b.Name.Contains(searchString) ||
                    b.Description.Contains(searchString));
            }

            if (campusId.HasValue && campusId.Value > 0)
            {
                buildings = buildings.Where(b => b.CampusId == campusId.Value);
            }

            ViewBag.CampusId = new SelectList(_context.Campuses, "CampusId", "Name");

            return View(await buildings.ToListAsync());
        }


        // GET: Buildings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var building = await _context.Buildings
                .Include(b => b.Campus)
                .FirstOrDefaultAsync(m => m.BuildingId == id);
            if (building == null)
            {
                return NotFound();
            }

            return View(building);
        }

        // GET: Buildings/Create
        public IActionResult Create()
        {
            ViewData["CampusId"] = new SelectList(_context.Campuses, "CampusId", "Name");
            return View();
        }

        // POST: Buildings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BuildingId,Name,Description,CampusId")] Building building)
        {
            // Check if a building with the same name exists in the selected campus
            bool exists = await _context.Buildings
                .AnyAsync(b => b.Name == building.Name && b.CampusId == building.CampusId);

            if (exists)
            {
                ModelState.AddModelError("Name", "A building with this name already exists in the selected campus.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(building);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CampusId"] = new SelectList(_context.Campuses, "CampusId", "Name", building.CampusId);
            return View(building);
        }

        // GET: Buildings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var building = await _context.Buildings.FindAsync(id);
            if (building == null)
            {
                return NotFound();
            }
            ViewData["CampusId"] = new SelectList(_context.Campuses, "CampusId", "Name", building.CampusId);
            return View(building);
        }

        // POST: Buildings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BuildingId,Name,Description,CampusId")] Building building)
        {
            if (id != building.BuildingId)
            {
                return NotFound();
            }

            // Check if a building with the same name already exists (excluding the current one)
            bool nameExists = await _context.Buildings
                .AnyAsync(b => b.Name == building.Name && b.BuildingId != building.BuildingId);

            if (nameExists)
            {
                ModelState.AddModelError("Name", "A building with this name already exists.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(building);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BuildingExists(building.BuildingId))
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

            ViewData["CampusId"] = new SelectList(_context.Campuses, "CampusId", "Name", building.CampusId);
            return View(building);
        }


        // GET: Buildings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var building = await _context.Buildings
                .Include(b => b.Campus)
                .FirstOrDefaultAsync(m => m.BuildingId == id);
            if (building == null)
            {
                return NotFound();
            }

            return View(building);
        }

        // POST: Buildings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var building = await _context.Buildings.FindAsync(id);
            if (building != null)
            {
                _context.Buildings.Remove(building);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BuildingExists(int id)
        {
            return _context.Buildings.Any(e => e.BuildingId == id);
        }
    }
}
