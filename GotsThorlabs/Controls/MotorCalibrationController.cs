using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using Microsoft.AspNetCore.Mvc;

namespace GotsThorlabs.Controls
{
    /// <summary>
    /// Caracterización mecánica del motor: cuántos nanómetros avanza realmente cada paso.
    ///
    /// El procedimiento es manual y largo (unos veinte minutos por eje midiendo con pie de
    /// rey), por lo que la API está pensada para ir registrando parada a parada y poder
    /// retomar más tarde, en lugar de recibir la medición completa de una sola vez.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class MotorCalibrationController : ControllerBase
    {
        private readonly IMotorCalibrationService _service;

        public MotorCalibrationController(IMotorCalibrationService service)
        {
            _service = service;
        }

        /// <summary>Caracterizaciones de un grupo de calibración, la vigente primero.</summary>
        [HttpGet("by-group/{groupCalibrationId}")]
        public async Task<ActionResult<IEnumerable<MotorCalibrationDTO>>> GetByGroupAsync(
            string groupCalibrationId, CancellationToken ct)
        {
            var items = await _service.GetByGroupAsync(groupCalibrationId, ct);
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MotorCalibrationDTO>> GetByIdAsync(string id, CancellationToken ct)
        {
            var item = await _service.GetByIdAsync(id, ct);
            if (item is null) return NotFound();
            return Ok(item);
        }

        /// <summary>Inicia una caracterización con ambos ejes pendientes. No mueve el motor.</summary>
        [HttpPost]
        public async Task<ActionResult<MotorCalibrationDTO>> CreateAsync(
            [FromBody] MotorCalibrationCreateDTO dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest();
            try
            {
                return Ok(await _service.CreateAsync(dto, ct));
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        /// <summary>
        /// Mueve un eje a una posición absoluta con los parámetros de accionamiento de esta
        /// caracterización. Puede tardar varios minutos: un tramo son decenas de miles de
        /// pasos.
        /// </summary>
        [HttpPost("{id}/move")]
        public async Task<IActionResult> MoveAsync(string id, [FromBody] MotorMoveDTO dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest();
            try
            {
                await _service.MoveAsync(id, dto, ct);
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (OperationCanceledException) { return StatusCode(499, "Movimiento cancelado."); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { return StatusCode(500, $"Error al mover el motor: {ex.Message}"); }
        }

        /// <summary>Registra la lectura del pie de rey de una parada y recalcula el eje.</summary>
        [HttpPost("{id}/measurement")]
        public async Task<ActionResult<AxisStepCalibrationDTO>> RecordMeasurementAsync(
            string id, [FromBody] AxisStepMeasurementCreateDTO dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest();
            try
            {
                return Ok(await _service.RecordMeasurementAsync(id, dto, ct));
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        /// <summary>Corrige una lectura ya registrada.</summary>
        [HttpPut("measurement/{measurementId}")]
        public async Task<ActionResult<AxisStepCalibrationDTO>> UpdateMeasurementAsync(
            string measurementId, [FromBody] AxisStepMeasurementUpdateDTO dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest();
            try
            {
                return Ok(await _service.UpdateMeasurementAsync(measurementId, dto, ct));
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [HttpDelete("measurement/{measurementId}")]
        public async Task<ActionResult<AxisStepCalibrationDTO>> DeleteMeasurementAsync(
            string measurementId, CancellationToken ct)
        {
            try
            {
                return Ok(await _service.DeleteMeasurementAsync(measurementId, ct));
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        }

        /// <summary>Marca esta caracterización como la vigente de su grupo.</summary>
        [HttpPost("{id}/accept")]
        public async Task<IActionResult> AcceptAsync(string id, CancellationToken ct)
        {
            try
            {
                await _service.AcceptAsync(id, ct);
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        }

        /// <summary>
        /// Copia esta caracterización a otro grupo. Útil cuando solo cambió el objetivo y
        /// el montaje mecánico es el mismo: evita repetir la medición con pie de rey.
        /// </summary>
        [HttpPost("{id}/copy-to/{targetGroupCalibrationId}")]
        public async Task<ActionResult<MotorCalibrationDTO>> CopyToGroupAsync(
            string id, string targetGroupCalibrationId, CancellationToken ct)
        {
            try
            {
                return Ok(await _service.CopyToGroupAsync(id, targetGroupCalibrationId, ct));
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(string id, CancellationToken ct)
        {
            try
            {
                await _service.DeleteAsync(id, ct);
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        }
    }
}
