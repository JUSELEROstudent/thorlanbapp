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

        [HttpGet("{id}")]
        public async Task<ActionResult<Camera>> GetByIdAsync(string id, CancellationToken ct)
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAsync(string id, [FromBody] CameraDTO dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest();
            if (id != dto.CameraId && !string.IsNullOrEmpty(dto.CameraId)) return BadRequest("Id mismatch");

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

        /// <summary>
        /// Parámetros de captura que acepta el dispositivo de esta cámara, con sus
        /// rangos y valores actuales, más lo que el usuario dejó guardado.
        /// Consulta el hardware, así que puede tardar unos segundos.
        /// </summary>
        [HttpGet("{id}/parameters")]
        public async Task<ActionResult<CameraParametersResponseDTO>> GetParametersAsync(string id, CancellationToken ct)
        {
            try
            {
                var result = await _service.GetParametersAsync(id, ct);
                return Ok(result);
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al consultar los parámetros de la cámara: {ex.Message}");
            }
        }

        /// <summary>
        /// Guarda los parámetros de captura de esta cámara. Se aplican de inmediato al
        /// driver y se usan en el streaming, en la calibración automática y en los
        /// recorridos.
        /// </summary>
        [HttpPut("{id}/parameters")]
        public async Task<IActionResult> UpdateParametersAsync(
            string id,
            [FromBody] CameraParametersUpdateDTO dto,
            CancellationToken ct)
        {
            if (dto is null) return BadRequest();

            try
            {
                await _service.UpdateParametersAsync(id, dto, ct);
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al guardar los parámetros de la cámara: {ex.Message}");
            }
            return NoContent();
        }
    }
}
