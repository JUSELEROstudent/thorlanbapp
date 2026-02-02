using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Database.EntityRepo.Entities;

namespace GotsThorlabs.Controls
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroupCalibrationController : ControllerBase
    {
        private readonly ThorlabsDbContext _db;

        public GroupCalibrationController(ThorlabsDbContext db)
        {
            _db = db;
        }

        // GET: api/GroupCalibration
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GroupCalibration>>> GetAllAsync(CancellationToken ct)
        {
            var groups = await _db.GroupCalibrations.AsNoTracking().ToListAsync(ct);
            return Ok(groups);
        }

        // GET: api/GroupCalibration/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<GroupCalibration>> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var group = await _db.GroupCalibrations.AsNoTracking().FirstOrDefaultAsync(g => g.GroupCailbrationId == id, ct);
            if (group is null) return NotFound();
            return Ok(group);
        }

        // POST: api/GroupCalibration
        [HttpPost]
        public async Task<ActionResult<GroupCalibration>> CreateAsync([FromBody] GroupCalibration group, CancellationToken ct)
        {
            if (group is null) return BadRequest();

            if (group.GroupCailbrationId == Guid.Empty)
            {
                group.GroupCailbrationId = Guid.NewGuid();
            }

            _db.GroupCalibrations.Add(group);
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return Conflict("No se puede crear el grupo de calibración por claves foráneas inválidas.");
            }

            return Ok(group);
        }

        // PUT: api/GroupCalibration/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] GroupCalibration update, CancellationToken ct)
        {
            if (update is null) return BadRequest();
            if (id != update.GroupCailbrationId && update.GroupCailbrationId != Guid.Empty) return BadRequest("Id mismatch");

            var existing = await _db.GroupCalibrations.FirstOrDefaultAsync(g => g.GroupCailbrationId == id, ct);
            if (existing is null) return NotFound();

            existing.CameraId = update.CameraId;
            existing.MicroscopeId = update.MicroscopeId;
            existing.IncreaseId = update.IncreaseId;
            existing.Date = update.Date;
            existing.AditionalInfo = update.AditionalInfo;

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return Conflict("No se puede actualizar el grupo de calibración por claves foráneas inválidas.");
            }

            return NoContent();
        }

        // DELETE: api/GroupCalibration/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
        {
            var group = await _db.GroupCalibrations.FirstOrDefaultAsync(g => g.GroupCailbrationId == id, ct);
            if (group is null) return NotFound();

            _db.GroupCalibrations.Remove(group);
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return Conflict("No se puede eliminar el grupo de calibración porque tiene dependencias.");
            }

            return NoContent();
        }
    }
}
