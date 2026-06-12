using Microsoft.AspNetCore.Mvc;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using GotsThorlabs.Database.EntityRepo.Entities;
using Microsoft.EntityFrameworkCore;

namespace GotsThorlabs.Controls
{
    [Route("api/[controller]")]
    [ApiController]
    public class PicsCalibrationController : ControllerBase
    {
        private readonly IPicsCalibrationService _service;

        public PicsCalibrationController(IPicsCalibrationService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PicsCalibration>>> GetAllAsync(CancellationToken ct)
        {
            var pics = await _service.GetAllAsync(ct);
            return Ok(pics);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PicsCalibration>> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var pic = await _service.GetByIdAsync(id, ct);
            if (pic is null) return NotFound();
            return Ok(pic);
        }

        [HttpPost]
        [RequestSizeLimit(50_000_000)]
        public async Task<ActionResult<PicsCalibration>> CreateAsync([FromForm] PicsCalibrationUploadDTO dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest();
            try
            {
                var entity = await _service.CreateAsync(dto, ct);
                return Ok(entity);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                return Conflict("No se puede crear la calibración por grupo inválido u otras restricciones.");
            }
        }

        [HttpPost("upload")]
        [RequestSizeLimit(50_000_000)]
        public async Task<ActionResult<PicsCalibration>> UploadAsync([FromForm] PicsCalibrationUploadDTO dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest();
            try
            {
                var entity = await _service.UploadAsync(dto, ct);
                return Ok(entity);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                return Conflict("No se pudo guardar la calibración en la base de datos.");
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] PicsCalibrationUpdateDTO dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest();
            if (id != dto.PicsCalibrationId && dto.PicsCalibrationId != Guid.Empty) return BadRequest("Id mismatch");

            try
            {
                await _service.UpdateAsync(id, dto, ct);
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                return Conflict("No se puede actualizar la calibración por restricciones de base de datos.");
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