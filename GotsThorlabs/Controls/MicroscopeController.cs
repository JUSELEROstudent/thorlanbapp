using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Database.EntityRepo.Entities;

namespace GotsThorlabs.Controls
{
    [Route("api/[controller]")]
    [ApiController]
    public class MicroscopeController : ControllerBase
    {
        private readonly ThorlabsDbContext _db;

        public MicroscopeController(ThorlabsDbContext db)
        {
            _db = db;
        }

        // GET: api/Microscope
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Microscope>>> GetAllAsync(CancellationToken ct)
        {
            var microscopes = await _db.Microscopes.AsNoTracking().ToListAsync(ct);
            return Ok(microscopes);
        }

        // GET: api/Microscope/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<Microscope>> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var microscope = await _db.Microscopes.AsNoTracking().FirstOrDefaultAsync(m => m.MicroscopeId == id, ct);
            if (microscope is null) return NotFound();
            return Ok(microscope);
        }

        // POST: api/Microscope
        [HttpPost]
        public async Task<ActionResult<Microscope>> CreateAsync([FromBody] Microscope microscope, CancellationToken ct)
        {
            if (microscope is null) return BadRequest();

            if (microscope.MicroscopeId == Guid.Empty)
            {
                microscope.MicroscopeId = Guid.NewGuid();
            }

            _db.Microscopes.Add(microscope);
            await _db.SaveChangesAsync(ct);

            return Ok(microscope);
        }

        // PUT: api/Microscope/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] Microscope update, CancellationToken ct)
        {
            if (update is null) return BadRequest();
            if (id != update.MicroscopeId && update.MicroscopeId != Guid.Empty) return BadRequest("Id mismatch");

            var existing = await _db.Microscopes.FirstOrDefaultAsync(m => m.MicroscopeId == id, ct);
            if (existing is null) return NotFound();

            existing.Name = update.Name;
            existing.Brand = update.Brand;
            existing.Site = update.Site;
            existing.AditionalInfo = update.AditionalInfo;

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        // DELETE: api/Microscope/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
        {
            var microscope = await _db.Microscopes.FirstOrDefaultAsync(m => m.MicroscopeId == id, ct);
            if (microscope is null) return NotFound();

            _db.Microscopes.Remove(microscope);
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return Conflict("No se puede eliminar el microscopio porque tiene dependencias.");
            }

            return NoContent();
        }
    }
}
