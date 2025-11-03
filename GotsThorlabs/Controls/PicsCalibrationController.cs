using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Database.EntityRepo.Entities;

namespace GotsThorlabs.Controls
{
    [Route("api/[controller]")]
    [ApiController]
    public class PicsCalibrationController : ControllerBase
    {
        private readonly ThorlabsDbContext _db;

        public PicsCalibrationController(ThorlabsDbContext db)
        {
            _db = db;
        }

        // GET: api/PicsCalibration
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PicsCalibration>>> GetAllAsync(CancellationToken ct)
        {
            var pics = await _db.PicsCalibrations.AsNoTracking().ToListAsync(ct);
            return Ok(pics);
        }

        // GET: api/PicsCalibration/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PicsCalibration>> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var pic = await _db.PicsCalibrations.AsNoTracking().FirstOrDefaultAsync(p => p.PicsCalibrationId == id, ct);
            if (pic is null) return NotFound();
            return Ok(pic);
        }

        // POST: api/PicsCalibration
        [HttpPost]
        public async Task<ActionResult<PicsCalibration>> CreateAsync([FromBody] PicsCalibration pic, CancellationToken ct)
        {
            if (pic is null) return BadRequest();

            if (pic.PicsCalibrationId == Guid.Empty)
            {
                pic.PicsCalibrationId = Guid.NewGuid();
            }

            _db.PicsCalibrations.Add(pic);
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return Conflict("No se puede crear la calibración por grupo inválido u otras restricciones.");
            }

            return CreatedAtAction(nameof(GetByIdAsync), new { id = pic.PicsCalibrationId }, pic);
        }

        // PUT: api/PicsCalibration/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] PicsCalibration update, CancellationToken ct)
        {
            if (update is null) return BadRequest();
            if (id != update.PicsCalibrationId && update.PicsCalibrationId != Guid.Empty) return BadRequest("Id mismatch");

            var existing = await _db.PicsCalibrations.FirstOrDefaultAsync(p => p.PicsCalibrationId == id, ct);
            if (existing is null) return NotFound();

            existing.GroupCailbrationId = update.GroupCailbrationId;
            existing.Pic1 = update.Pic1;
            existing.Pic2 = update.Pic2;
            existing.AxeDirectionCalibration = update.AxeDirectionCalibration;
            existing.Acepted = update.Acepted;
            existing.dx = update.dx;
            existing.dy = update.dy;
            existing.Confidence = update.Confidence;
            existing.MeasureUnit = update.MeasureUnit;
            existing.MovementValue = update.MovementValue;

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return Conflict("No se puede actualizar la calibración por restricciones de base de datos.");
            }

            return NoContent();
        }

        // DELETE: api/PicsCalibration/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
        {
            var pic = await _db.PicsCalibrations.FirstOrDefaultAsync(p => p.PicsCalibrationId == id, ct);
            if (pic is null) return NotFound();

            _db.PicsCalibrations.Remove(pic);
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return Conflict("No se puede eliminar la calibración por restricciones de base de datos.");
            }

            return NoContent();
        }
    }
}
