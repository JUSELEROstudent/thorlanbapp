using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Database.EntityRepo.Entities;

namespace GotsThorlabs.Controls
{
    [Route("api/[controller]")]
    [ApiController]
    public class CameraController : ControllerBase
    {
        private readonly ThorlabsDbContext _db;

        public CameraController(ThorlabsDbContext db)
        {
            _db = db;
        }

        // GET: api/Camera
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Camera>>> GetAllAsync(CancellationToken ct)
        {
            var cameras = await _db.Cameras.AsNoTracking().ToListAsync(ct);
            return Ok(cameras);
        }

        // GET: api/Camera/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<Camera>> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var camera = await _db.Cameras.AsNoTracking().FirstOrDefaultAsync(c => c.CameraId == id, ct);
            if (camera is null) return NotFound();
            return Ok(camera);
        }

        // POST: api/Camera
        [HttpPost]
        public async Task<ActionResult<Camera>> CreateAsync([FromBody] Camera camera, CancellationToken ct)
        {
            if (camera is null) return BadRequest();

            if (camera.CameraId == Guid.Empty)
            {
                camera.CameraId = Guid.NewGuid();
            }

            _db.Cameras.Add(camera);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetByIdAsync), new { id = camera.CameraId }, camera);
        }

        // PUT: api/Camera/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] Camera update, CancellationToken ct)
        {
            if (update is null) return BadRequest();
            if (id != update.CameraId && update.CameraId != Guid.Empty) return BadRequest("Id mismatch");

            var existing = await _db.Cameras.FirstOrDefaultAsync(c => c.CameraId == id, ct);
            if (existing is null) return NotFound();

            existing.Name = update.Name;
            existing.LocalIdentifier = update.LocalIdentifier;
            existing.Features = update.Features;

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        // DELETE: api/Camera/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
        {
            var camera = await _db.Cameras.FirstOrDefaultAsync(c => c.CameraId == id, ct);
            if (camera is null) return NotFound();

            _db.Cameras.Remove(camera);
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return Conflict("No se puede eliminar la cámara porque tiene dependencias.");
            }

            return NoContent();
        }
    }
}
