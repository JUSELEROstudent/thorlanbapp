using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using Microsoft.AspNetCore.Mvc;

namespace GotsThorlabs.Controls
{
    [Route("api/[controller]")]
    [ApiController]
    public class FocusController : ControllerBase
    {
        private readonly IFocusService _service;

        public FocusController(IFocusService service)
        {
            _service = service;
        }

        /// <summary>
        /// Captura un cuadro de la cámara indicada y devuelve su medida de nitidez.
        /// El frontend lo usa antes de lanzar una calibración automática para avisar
        /// si la muestra está desenfocada.
        /// </summary>
        [HttpPost("evaluate")]
        public async Task<ActionResult<FocusEvaluationDTO>> EvaluateAsync(
            [FromQuery] string cameraName,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(cameraName))
                return BadRequest("cameraName es requerido.");

            try
            {
                var result = await _service.EvaluateAsync(cameraName, ct);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al evaluar el enfoque: {ex.Message}");
            }
        }
    }
}
