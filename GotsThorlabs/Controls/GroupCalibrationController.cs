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

        /// <summary>
        /// Grupos de calibración con el nombre de la cámara, el microscopio y el objetivo
        /// ya resueltos. La vista los muestra tal cual; no necesita cruzar identificadores
        /// contra otras listas para saber a qué equipos pertenece cada grupo.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GroupCalibrationResponseDTO>>> GetAllAsync(CancellationToken ct)
        {
            var groups = await _service.GetAllAsync(ct);
            return Ok(groups);
        }

        /// <summary>
        /// Grupos de calibración con la cámara, el microscopio y el objetivo asociados,
        /// más sus mediciones. Es lo que consume la vista de calibraciones: permite
        /// mostrar a qué combinación de equipos pertenece cada medición sin hacer una
        /// petición por grupo.
        /// </summary>
        [HttpGet("with-details")]
        public async Task<ActionResult<IEnumerable<GroupCalibration>>> GetAllWithDetailsAsync(CancellationToken ct)
        {
            var groups = await _service.GetAllWithDetailsAsync(ct);
            return Ok(groups);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GroupCalibrationResponseDTO>> GetByIdAsync(string id, CancellationToken ct)
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAsync(string id, [FromBody] GroupCalibrationDTO dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest();
            if (id != dto.GroupCailbrationId && !string.IsNullOrEmpty(dto.GroupCailbrationId)) return BadRequest("Id mismatch");

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
