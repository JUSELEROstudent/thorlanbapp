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

        public TourController(ITourCrudService service)
        {
            _service = service;
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