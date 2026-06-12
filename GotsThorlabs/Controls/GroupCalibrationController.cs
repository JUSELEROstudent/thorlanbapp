using Microsoft.AspNetCore.Mvc;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using GotsThorlabs.Database.EntityRepo.Entities;
using Microsoft.EntityFrameworkCore;

namespace GotsThorlabs.Controls
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroupCalibrationController : ControllerBase
    {
        private readonly IGroupCalibrationService _service;

        public GroupCalibrationController(IGroupCalibrationService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<GroupCalibration>>> GetAllAsync(CancellationToken ct)
        {
            var groups = await _service.GetAllAsync(ct);
            return Ok(groups);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<GroupCalibration>> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var group = await _service.GetByIdAsync(id, ct);
            if (group is null) return NotFound();
            return Ok(group);
        }

        [HttpPost]
        public async Task<ActionResult<GroupCalibration>> CreateAsync([FromBody] GroupCalibrationDTO dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest();
            try
            {
                var entity = await _service.CreateAsync(dto, ct);
                return Ok(entity);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                return Conflict("No se puede crear el grupo de calibración por claves foráneas inválidas.");
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] GroupCalibrationDTO dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest();
            if (id != dto.GroupCailbrationId && dto.GroupCailbrationId != Guid.Empty) return BadRequest("Id mismatch");

            try
            {
                await _service.UpdateAsync(id, dto, ct);
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                return Conflict("No se puede actualizar el grupo de calibración por claves foráneas inválidas.");
            }
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