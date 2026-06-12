using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;

namespace GotsThorlabs.Controls
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhaseCorrelationCalibrationController : ControllerBase
    {
        private readonly IPhaseCorrelationService _service;

        public PhaseCorrelationCalibrationController(IPhaseCorrelationService service)
        {
            _service = service;
        }

        [HttpPost]
        public IActionResult CreatePhaseCorrelationCalibration(string img1, string img2)
        {
            try
            {
                var result = _service.DetectShiftFromPaths(img1, img2);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error procesando imágenes: {ex.Message}");
            }
        }

        [HttpPost("file-data")]
        [Consumes("multipart/form-data")]
        public IActionResult CreatePhaseCorrelationCalibrationFromFiles(IFormFile img1, IFormFile img2)
        {
            if (img1 == null || img1.Length == 0)
                return BadRequest("img1 no fue enviado o está vacío.");
            if (img2 == null || img2.Length == 0)
                return BadRequest("img2 no fue enviado o está vacío.");

            try
            {
                using var stream1 = img1.OpenReadStream();
                using var stream2 = img2.OpenReadStream();
                var result = _service.DetectShiftFromFiles(stream1, stream2);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error procesando imágenes: {ex.Message}");
            }
        }
    }
}