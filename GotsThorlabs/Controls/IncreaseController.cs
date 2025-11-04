using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Database.EntityRepo.Entities;

namespace GotsThorlabs.Controls
{
    [Route("api/[controller]")]
    [ApiController]
    public class IncreaseController : ControllerBase
    {
        private readonly ThorlabsDbContext _db;

        public IncreaseController(ThorlabsDbContext db)
        {
            _db = db;
        }

        // GET: api/Increase
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Increase>>> GetAllAsync(CancellationToken ct)
        {
            var increases = await _db.Increases.AsNoTracking().ToListAsync(ct);
            return Ok(increases);
        }

        // GET: api/Increase/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<Increase>> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var increase = await _db.Increases.AsNoTracking().FirstOrDefaultAsync(i => i.IncreaseId == id, ct);
            if (increase is null) return NotFound();
            return Ok(increase);
        }

        // POST: api/Increase
        [HttpPost]
        public async Task<ActionResult<Increase>> CreateAsync([FromBody] Increase increase, CancellationToken ct)
        {
            if (increase is null) return BadRequest();

            if (increase.IncreaseId == Guid.Empty)
            {
                increase.IncreaseId = Guid.NewGuid();
            }

            var savedValue = _db.Increases.Add(increase);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(increase), new { id = increase.IncreaseId }, increase);
        }

        // PUT: api/Increase/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] Increase update, CancellationToken ct)
        {
            if (update is null) return BadRequest();
            if (id != update.IncreaseId && update.IncreaseId != Guid.Empty) return BadRequest("Id mismatch");

            var existing = await _db.Increases.FirstOrDefaultAsync(i => i.IncreaseId == id, ct);
            if (existing is null) return NotFound();

            existing.Name = update.Name;
            existing.Value = update.Value;
            existing.AditionalInfo = update.AditionalInfo;

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        // DELETE: api/Increase/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
        {
            var increase = await _db.Increases.FirstOrDefaultAsync(i => i.IncreaseId == id, ct);
            if (increase is null) return NotFound();

            _db.Increases.Remove(increase);
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return Conflict("No se puede eliminar el aumento porque tiene dependencias.");
            }

            return NoContent();
        }
    }
}
