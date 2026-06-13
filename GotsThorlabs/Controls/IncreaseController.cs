using Microsoft.AspNetCore.Mvc;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using GotsThorlabs.Database.EntityRepo.Entities;

namespace GotsThorlabs.Controls
{
    [Route("api/[controller]")]
    [ApiController]
    public class IncreaseController : ControllerBase
    {
        private readonly IIncreaseService _service;

        public IncreaseController(IIncreaseService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Increase>>> GetAllAsync(CancellationToken ct)
        {
            var increases = await _service.GetAllAsync(ct);
            return Ok(increases);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Increase>> GetByIdAsync(string id, CancellationToken ct)
        {
            var increase = await _service.GetByIdAsync(id, ct);
            if (increase is null) return NotFound();
            return Ok(increase);
        }

        [HttpPost]
        public async Task<ActionResult<Increase>> CreateAsync([FromBody] IncreaseDTO dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest();
            var entity = await _service.CreateAsync(dto, ct);
            return Ok(entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAsync(string id, [FromBody] IncreaseDTO dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest();
            if (id != dto.IncreaseId && !string.IsNullOrEmpty(dto.IncreaseId)) return BadRequest("Id mismatch");

            try
            {
                await _service.UpdateAsync(id, dto, ct);
            }
            catch (KeyNotFoundException) { return NotFound(); }
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(string id, CancellationToken ct)
        {
            try
            {
                await _service.DeleteAsync(id, ct);
            }
            catch (KeyNotFoundException) { return NotFound(); }
            return NoContent();
        }
    }
}
