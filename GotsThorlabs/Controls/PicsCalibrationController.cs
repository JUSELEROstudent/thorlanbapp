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
        private readonly IPhaseCorrelationService _phaseCorrelationService;
        private readonly ICameraService _cameraService;

        public PicsCalibrationController(
            IPicsCalibrationService service,
            IPhaseCorrelationService phaseCorrelationService,
            ICameraService cameraService)
        {
            _service = service;
            _phaseCorrelationService = phaseCorrelationService;
            _cameraService = cameraService;
        }

        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<PicsCalibration>>> GetAllAsync( CancellationToken ct)
        //{
        //    var pics = await _service.GetAllAsync(ct);
        //    return Ok(pics);
        //}

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PicsCalibration>>> GetByGroupCalibrationIdAsync([FromQuery] string groupCailbrationId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(groupCailbrationId)) return BadRequest("groupCailbrationId es requerido");
            var pics = await _service.GetByGroupCalibrationIdAsync(groupCailbrationId, ct);
            return Ok(pics);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PicsCalibration>> GetByIdAsync(string id, CancellationToken ct)
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

        [HttpPost("auto-calibration")]
        public async Task<ActionResult<List<PicsCalibration>>> RunAutoCalibrationAsync(
            [FromQuery] string kimDeviceId,
            [FromQuery] string groupCalibrationId,
            [FromQuery] string axis,
            [FromQuery] string? localIdentifier,
            CancellationToken ct,
            [FromQuery] string? magnitudes = null,
            [FromQuery] int repetitions = 1)
        {
            if (string.IsNullOrWhiteSpace(kimDeviceId) ||
                string.IsNullOrWhiteSpace(groupCalibrationId) ||
                string.IsNullOrWhiteSpace(axis))
            {
                return BadRequest("kimDeviceId, groupCalibrationId y axis son requeridos.");
            }

            if (axis.ToLower() != "x" && axis.ToLower() != "y")
            {
                return BadRequest("El eje debe ser 'x' o 'y'.");
            }

            // magnitudes: lista de pasos separados por coma, admite negativos, p. ej.
            // "-1000,-100,-10,-1,1,10,100,1000". Si se omite, el servicio usa el valor
            // histórico (0, 1, 10, 100, 1000, solo avance).
            int[]? parsedMagnitudes = new[] { -1000, -100, -10, -1, 1, 10, 100, 1000 };
            if (!string.IsNullOrWhiteSpace(magnitudes))
            {
                var parts = magnitudes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var parsedList = new List<int>();
                foreach (var part in parts)
                {
                    if (!int.TryParse(part, out var value))
                    {
                        return BadRequest($"magnitudes contiene un valor no numérico: '{part}'. Use enteros separados por coma, p. ej. -1000,-10,10,1000.");
                    }
                    parsedList.Add(value);
                }
                if (parsedList.Count == 0)
                {
                    return BadRequest("magnitudes no puede quedar vacío si se especifica.");
                }
                parsedMagnitudes = parsedList.ToArray();
            }

            if (repetitions < 1)
            {
                return BadRequest("repetitions debe ser al menos 1.");
            }

            try
            {
                var results = await _service.RunAutoCalibrationAsync(
                    kimDeviceId,
                    groupCalibrationId,
                    axis,
                    _cameraService,
                    localIdentifier,
                    _phaseCorrelationService,
                    ct,
                    parsedMagnitudes,
                    repetitions);

                return Ok(results);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error durante la calibración automática: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAsync(string id, [FromBody] PicsCalibrationUpdateDTO dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest();
            if (id != dto.PicsCalibrationId && !string.IsNullOrEmpty(dto.PicsCalibrationId)) return BadRequest("Id mismatch");

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

        [HttpGet("image")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCalibrationImage([FromQuery] string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return BadRequest("El parámetro filePath es requerido");
            }

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound($"Archivo no encontrado: {filePath}");
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".bmp" => "image/bmp",
                _ => "application/octet-stream"
            };

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var fileName = Path.GetFileName(filePath);

            return File(fileBytes, contentType, fileName);
        }
    }
}
