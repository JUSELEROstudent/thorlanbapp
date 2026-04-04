using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Database.EntityRepo.Entities;

namespace GotsThorlabs.Controls
{
    [Route("api/[controller]")]
    [ApiController]
    public class TourController : ControllerBase
    {
        private readonly ThorlabsDbContext _db;

        public TourController(ThorlabsDbContext db)
        {
            _db = db;
        }

        // GET: api/Tour
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tour>>> GetAllAsync(CancellationToken ct)
        {
            var tours = await _db.Tours.AsNoTracking().ToListAsync(ct);
            return Ok(tours);
        }

        // GET: api/Tour/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Tour>> GetByIdAsync(int id, CancellationToken ct)
        {
            var tour = await _db.Tours.AsNoTracking().FirstOrDefaultAsync(t => t.IdTour == id, ct);
            if (tour is null) return NotFound();
            return Ok(tour);
        }

        // POST: api/Tour
        [HttpPost]
        public async Task<ActionResult<Tour>> CreateAsync([FromBody] Tour tour, CancellationToken ct)
        {
            if (tour is null) return BadRequest();

            // Basic validation
            if (string.IsNullOrWhiteSpace(tour.NameFolder))
                return BadRequest("El campo NameFolder es requerido.");

            // Date comes as DateTime in EF entity; ensure not default
            if (tour.Date == default)
                tour.Date = DateTime.UtcNow;

            _db.Tours.Add(tour);
            await _db.SaveChangesAsync(ct);

            return Ok(tour);
        }

        // PUT: api/Tour/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateAsync(int id, [FromBody] Tour update, CancellationToken ct)
        {
            if (update is null) return BadRequest();
            if (id != update.IdTour && update.IdTour != 0) return BadRequest("Id mismatch");

            var existing = await _db.Tours.FirstOrDefaultAsync(t => t.IdTour == id, ct);
            if (existing is null) return NotFound();

            // Update scalar fields
            existing.Date = update.Date == default ? existing.Date : update.Date;
            existing.NameFolder = update.NameFolder;
            existing.NumberX = update.NumberX;
            existing.NumberY = update.NumberY;
            existing.NumberZ = update.NumberZ;
            existing.Camera = update.Camera;
            existing.EndStatus = update.EndStatus;

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        // DELETE: api/Tour/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteAsync(int id, CancellationToken ct)
        {
            var tour = await _db.Tours.FirstOrDefaultAsync(t => t.IdTour == id, ct);
            if (tour is null) return NotFound();

            _db.Tours.Remove(tour);
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return Conflict("No se puede eliminar el tour porque tiene dependencias.");
            }

            return NoContent();
        }
    }
}