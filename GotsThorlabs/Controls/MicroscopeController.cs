using Microsoft.AspNetCore.Mvc;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using GotsThorlabs.Database.EntityRepo.Entities;

namespace GotsThorlabs.Controls
{
    [Route("api/[controller]")]
    [ApiController]
    public class MicroscopeController : ControllerBase
    {
        private readonly IMicroscopeService _service;

        public MicroscopeController(IMicroscopeService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Microscope>>> GetAllAsync(CancellationToken ct)
        {
            var microscopes = await _service.GetAllAsync(ct);
            return Ok(microscopes);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Microscope>> GetByIdAsync(string id, CancellationToken ct)
        {
            var microscope = await _service.GetByIdAsync(id, ct);
            if (microscope is null) return NotFound();
            return Ok(microscope);
        }

        [HttpPost]
        public async Task<ActionResult<Microscope>> CreateAsync([FromBody] MicroscopeDTO dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest();
            var entity = await _service.CreateAsync(dto, ct);
            return Ok(entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAsync(string id, [FromBody] MicroscopeDTO dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest();
            if (id != dto.MicroscopeId && !string.IsNullOrEmpty(dto.MicroscopeId)) return BadRequest("Id mismatch");

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
