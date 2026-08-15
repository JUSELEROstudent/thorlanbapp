using Microsoft.AspNetCore.Mvc;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using TourEntity = GotsThorlabs.Database.EntityRepo.Entities.Tour;

namespace GotsThorlabs.Controls
{
    [Route("api/[controller]")]
    [ApiController]
    public class TourController : ControllerBase
    {
        private readonly ITourCrudService _service;
        private readonly ITourPlanningService _planning;

        public TourController(ITourCrudService service, ITourPlanningService planning)
        {
            _service = service;
            _planning = planning;
        }

        /// <summary>
        /// Recorridos existentes, del más reciente al más antiguo, indicando cuáles
        /// tienen el stitching ya generado (hasStitching / stitchingUrl).
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TourResponseDTO>>> GetAllAsync(CancellationToken ct)
        {
            var tours = await _service.GetAllAsync(ct);
            return Ok(tours);
        }

        /// <summary>
        /// Estima cuánto tardará un recorrido con la configuración indicada, sin lanzarlo.
        /// Sondea la cámara una vez para conocer la resolución, así que puede tardar un
        /// segundo en responder.
        /// </summary>
        [HttpGet("estimate")]
        public async Task<ActionResult<GotsThorlabs.BLL.TourTimeEstimator.Estimate>> EstimateAsync(
            [FromQuery] string groupCalibrationId,
            [FromQuery] decimal areaXmm,
            [FromQuery] decimal areaYmm,
            CancellationToken ct,
            [FromQuery] int sweepPattern = (int)GotsThorlabs.BLL.SweepPattern.SerpentineScaled)
        {
            var pattern = Enum.IsDefined(typeof(GotsThorlabs.BLL.SweepPattern), sweepPattern)
                ? (GotsThorlabs.BLL.SweepPattern)sweepPattern
                : GotsThorlabs.BLL.SweepPattern.SerpentineScaled;

            try
            {
                return Ok(await _planning.EstimateAsync(groupCalibrationId, areaXmm, areaYmm, pattern, ct));
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { return StatusCode(500, $"Error al estimar el recorrido: {ex.Message}"); }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TourEntity>> GetByIdAsync(int id, CancellationToken ct)
        {
            var tour = await _service.GetByIdAsync(id, ct);
            if (tour is null) return NotFound();
            return Ok(tour);
        }

        [HttpPost]
        public async Task<ActionResult<TourEntity>> CreateAsync([FromBody] TourDTO dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest();
            try
            {
                var entity = await _service.CreateAsync(dto, ct);
                return Ok(entity);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateAsync(int id, [FromBody] TourDTO dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest();

            try
            {
                await _service.UpdateAsync(id, dto, ct);
            }
            catch (KeyNotFoundException) { return NotFound(); }
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteAsync(int id, CancellationToken ct)
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