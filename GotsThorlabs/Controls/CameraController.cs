using Microsoft.AspNetCore.Mvc;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using GotsThorlabs.Database.EntityRepo.Entities;

namespace GotsThorlabs.Controls
{
    [Route("api/[controller]")]
    [ApiController]
    public class CameraController : ControllerBase
    {
        private readonly ICameraCrudService _service;

        public CameraController(ICameraCrudService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Camera>>> GetAllAsync(CancellationToken ct)
        {
            var cameras = await _service.GetAllAsync(ct);
            return Ok(cameras);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<Camera>> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var camera = await _service.GetByIdAsync(id, ct);
            if (camera is null) return NotFound();
            return Ok(camera);
        }

        [HttpPost]
        public async Task<ActionResult<Camera>> CreateAsync([FromBody] CameraDTO dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest();
            try
            {
                var entity = await _service.CreateAsync(dto, ct);
                return Ok(entity);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] CameraDTO dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest();
            if (id != dto.CameraId && dto.CameraId != Guid.Empty) return BadRequest("Id mismatch");

            try
            {
                await _service.UpdateAsync(id, dto, ct);
            }
            catch (KeyNotFoundException) { return NotFound(); }
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
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