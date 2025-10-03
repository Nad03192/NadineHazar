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
    public class RoomsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoomsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Rooms
        public async Task<IActionResult> Index(string searchString, int? campusId, int? buildingId)
        {
            var query = _context.Rooms
                .Include(r => r.Building)
                .ThenInclude(b => b.Campus)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(r => r.Name.Contains(searchString) || r.Description.Contains(searchString));
            }

            if (campusId.HasValue)
            {
                query = query.Where(r => r.Building.CampusId == campusId.Value);
            }

            if (buildingId.HasValue)
            {
                query = query.Where(r => r.BuildingId == buildingId.Value);
            }

            ViewData["CurrentFilter"] = searchString;

            // Dropdown lists
            ViewBag.Campuses = new SelectList(_context.Campuses, "CampusId", "Name", campusId);
            ViewBag.Buildings = new SelectList(_context.Buildings
                .Where(b => !campusId.HasValue || b.CampusId == campusId.Value),
                "BuildingId", "Name", buildingId);

            return View(await query.ToListAsync());
        }

        // GET: Rooms/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .Include(r => r.Building)
                .FirstOrDefaultAsync(m => m.RoomId == id);
            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        // GET: Rooms/Create
        public IActionResult Create()
        {
            ViewData["BuildingId"] = new SelectList(_context.Buildings, "BuildingId", "Name");
            return View();
        }

        // POST: Rooms/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoomId,Name,Description,Capacity,BuildingId")] Room room)
        {
            // ✅ Check if a room with the same name already exists in this building
            bool exists = await _context.Rooms
                .AnyAsync(r =>  r.Name == room.Name);

            if (exists)
            {
                ModelState.AddModelError("Name", "A room with this name already exists in the selected building.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(room);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["BuildingId"] = new SelectList(_context.Buildings, "BuildingId", "Name", room.BuildingId);
            return View(room);
        }


        // GET: Rooms/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }
            ViewData["BuildingId"] = new SelectList(_context.Buildings, "BuildingId", "Name", room.BuildingId);
            return View(room);
        }

        // POST: Rooms/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RoomId,Name,Description,Capacity,BuildingId")] Room room)
        {
            if (id != room.RoomId)
            {
                return NotFound();
            }

            // ✅ Check uniqueness excluding the current room
            bool exists = await _context.Rooms
                .AnyAsync(r =>  r.Name == room.Name && r.RoomId != room.RoomId);

            if (exists)
            {
                ModelState.AddModelError("Name", "A room with this name already exists in the selected building.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(room);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoomExists(room.RoomId))
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

            ViewData["BuildingId"] = new SelectList(_context.Buildings, "BuildingId", "Name", room.BuildingId);
            return View(room);
        }

        // GET: Rooms/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .Include(r => r.Building)
                .FirstOrDefaultAsync(m => m.RoomId == id);
            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        // POST: Rooms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room != null)
            {
                _context.Rooms.Remove(room);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RoomExists(int id)
        {
            return _context.Rooms.Any(e => e.RoomId == id);
        }
    }
}
