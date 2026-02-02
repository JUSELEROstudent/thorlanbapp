using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Database.EntityRepo.Entities;
using GotsThorlabs.Models;
using Microsoft.Extensions.FileProviders;

namespace GotsThorlabs.Controls
{
    [Route("api/[controller]")]
    [ApiController]
    public class PicsCalibrationController : ControllerBase
    {
        private readonly ThorlabsDbContext _db;
        private readonly IWebHostEnvironment _env;

        public PicsCalibrationController(ThorlabsDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // GET: api/PicsCalibration
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PicsCalibration>>> GetAllAsync(CancellationToken ct)
        {
            var pics = await _db.PicsCalibrations.AsNoTracking().ToListAsync(ct);
            return Ok(pics);
        }

        // GET: api/PicsCalibration/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PicsCalibration>> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var pic = await _db.PicsCalibrations.AsNoTracking().FirstOrDefaultAsync(p => p.PicsCalibrationId == id, ct);
            if (pic is null) return NotFound();
            return Ok(pic);
        }

        // POST: api/PicsCalibration (JSON)
        [HttpPost]
        public async Task<ActionResult<PicsCalibration>> CreateAsync([FromBody] PicsCalibration pic, CancellationToken ct)
        {
            if (pic is null) return BadRequest();

            if (pic.PicsCalibrationId == Guid.Empty)
            {
                pic.PicsCalibrationId = Guid.NewGuid();
            }

            _db.PicsCalibrations.Add(pic);
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return Conflict("No se puede crear la calibración por grupo inválido u otras restricciones.");
            }

            return CreatedAtAction(nameof(GetByIdAsync), new { id = pic.PicsCalibrationId }, pic);
        }

        // POST: api/PicsCalibration/upload (multipart/form-data)
        [HttpPost("upload")]
        [RequestSizeLimit(50_000_000)] // 50 MB
        public async Task<ActionResult<PicsCalibration>> UploadAsync([FromForm] PicsCalibrationUploadDTO dto, CancellationToken ct)
        {
            if (dto is null || dto.Pic1File is null || dto.Pic2File is null)
                return BadRequest("Debe enviar Pic1File y Pic2File");

            // Ruta física donde se guardarán las imágenes
            var staticRoot = Path.Combine(_env.ContentRootPath, "StaticFiles", "pics-calibrations");
            Directory.CreateDirectory(staticRoot);

            // Nombre único para la carpeta del registro
            var id = Guid.NewGuid();
            var recordDir = Path.Combine(staticRoot, id.ToString());
            Directory.CreateDirectory(recordDir);

            // Guardar archivos
            string SaveFile(IFormFile f, string name)
            {
                var ext = Path.GetExtension(f.FileName);
                var safeName = name + ext;
                var fullPath = Path.Combine(recordDir, safeName);
                using var stream = System.IO.File.Create(fullPath);
                f.CopyTo(stream);
                return fullPath;
            }

            SaveFile(dto.Pic1File, "pic1");
            SaveFile(dto.Pic2File, "pic2");

            // Construir rutas accesibles vía HTTP
            var baseRequestPath = "/SouerceStaticFiles/pics-calibrations/" + id.ToString();
            var pic1Url = baseRequestPath + "/pic1" + Path.GetExtension(dto.Pic1File.FileName);
            var pic2Url = baseRequestPath + "/pic2" + Path.GetExtension(dto.Pic2File.FileName);

            var entity = new PicsCalibration
            {
                PicsCalibrationId = id,
                GroupCailbrationId = dto.GroupCailbrationId,
                Pic1 = pic1Url,
                Pic2 = pic2Url,
                AxeDirectionCalibration = dto.AxeDirectionCalibration,
                Acepted = dto.Acepted,
                dx = dto.dx,
                dy = dto.dy,
                Confidence = dto.Confidence,
                MeasureUnit = dto.MeasureUnit,
                MovementValue = dto.MovementValue
            };

            _db.PicsCalibrations.Add(entity);
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return Conflict("No se pudo guardar la calibración en la base de datos.");
            }

            return Ok(entity);
        }

        // PUT: api/PicsCalibration/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] PicsCalibration update, CancellationToken ct)
        {
            if (update is null) return BadRequest();
            if (id != update.PicsCalibrationId && update.PicsCalibrationId != Guid.Empty) return BadRequest("Id mismatch");

            var existing = await _db.PicsCalibrations.FirstOrDefaultAsync(p => p.PicsCalibrationId == id, ct);
            if (existing is null) return NotFound();

            existing.GroupCailbrationId = update.GroupCailbrationId;
            existing.Pic1 = update.Pic1;
            existing.Pic2 = update.Pic2;
            existing.AxeDirectionCalibration = update.AxeDirectionCalibration;
            existing.Acepted = update.Acepted;
            existing.dx = update.dx;
            existing.dy = update.dy;
            existing.Confidence = update.Confidence;
            existing.MeasureUnit = update.MeasureUnit;
            existing.MovementValue = update.MovementValue;

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return Conflict("No se puede actualizar la calibración por restricciones de base de datos.");
            }

            return NoContent();
        }

        // DELETE: api/PicsCalibration/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
        {
            var pic = await _db.PicsCalibrations.FirstOrDefaultAsync(p => p.PicsCalibrationId == id, ct);
            if (pic is null) return NotFound();

            _db.PicsCalibrations.Remove(pic);
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return Conflict("No se puede eliminar la calibración por restricciones de base de datos.");
            }

            return NoContent();
        }
    }
}
