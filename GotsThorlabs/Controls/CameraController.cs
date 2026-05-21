using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Database.EntityRepo.Entities;
using GotsThorlabs.Services;

namespace GotsThorlabs.Controls
{
    [Route("api/[controller]")]
    [ApiController]
    public class CameraController : ControllerBase
    {
        private readonly ThorlabsDbContext _db;
        private readonly CameraServiceFactory _cameraFactory;

        public CameraController(ThorlabsDbContext db, CameraServiceFactory cameraFactory)
        {
            _db = db;
            _cameraFactory = cameraFactory;
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

            camera.DriverType = string.IsNullOrWhiteSpace(camera.DriverType) ? "generic" : camera.DriverType.Trim().ToLowerInvariant();

            if (camera.CameraId == Guid.Empty)
            {
                camera.CameraId = Guid.NewGuid();
            }

            // Normalize LocalIdentifier: resolve raw value (moniker, index, etc.)
            // to the stable identifier (SerialNumber for IDS, numeric index for DirectShow).
            if (!string.IsNullOrWhiteSpace(camera.LocalIdentifier))
            {
                var resolved = _cameraFactory.ResolveLocalIdentifier(camera.DriverType, camera.LocalIdentifier);
                if (resolved is not null)
                    camera.LocalIdentifier = resolved;
                // If not resolved, keep the original value so it is not silently dropped.
            }

            var cameraExist = _db.Cameras.Where(item => item.Name.Trim().ToLower() == camera.Name.Trim().ToLower());
            if(cameraExist.Any())
            {
                BadRequest("Ya existe una cámara con ese nombre.");
            }

            _db.Cameras.Add(camera);
            await _db.SaveChangesAsync(ct);

            return Ok(camera);
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
            existing.DriverType = string.IsNullOrWhiteSpace(update.DriverType) ? "generic" : update.DriverType.Trim().ToLowerInvariant();

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
