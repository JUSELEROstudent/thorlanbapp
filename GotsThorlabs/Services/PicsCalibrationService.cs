using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Database.EntityRepo.Entities;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace GotsThorlabs.Services
{
    public class PicsCalibrationService : IPicsCalibrationService
    {
        private readonly ThorlabsDbContext _db;
        private readonly string _imagesBasePath;

        public PicsCalibrationService(ThorlabsDbContext db, IWebHostEnvironment env, IConfiguration configuration)
        {
            _db = db;
            _imagesBasePath = configuration["CalibrationImagesPath"]
                ?? Path.Combine(env.ContentRootPath, "StaticFiles", "pics-calibrations");
        }

        public async Task<IEnumerable<PicsCalibration>> GetAllAsync(CancellationToken ct)
        {
            return await _db.PicsCalibrations.AsNoTracking().ToListAsync(ct);
        }

        public async Task<PicsCalibration?> GetByIdAsync(string id, CancellationToken ct)
        {
            return await _db.PicsCalibrations.AsNoTracking().FirstOrDefaultAsync(p => p.PicsCalibrationId == id, ct);
        }

        public async Task<PicsCalibration> CreateAsync(PicsCalibrationUploadDTO dto, CancellationToken ct)
        {
            if (dto.Pic1File is null || dto.Pic2File is null)
                throw new ArgumentException("Debe enviar Pic1File y Pic2File.");

            var id = Guid.NewGuid().ToString();
            var recordDir = Path.Combine(_imagesBasePath, id);
            Directory.CreateDirectory(recordDir);

            var pic1Path = SaveFile(dto.Pic1File, recordDir, "pic1");
            var pic2Path = SaveFile(dto.Pic2File, recordDir, "pic2");

            var entity = new PicsCalibration
            {
                PicsCalibrationId = id,
                GroupCailbrationId = dto.GroupCailbrationId,
                Pic1 = pic1Path,
                Pic2 = pic2Path,
                AxeDirectionCalibration = dto.AxeDirectionCalibration,
                Acepted = dto.Acepted ? 1 : 0   ,
                Dx = dto.Dx,
                Dy = dto.Dy,
                Confidence = dto.Confidence,
                MeasureUnit = dto.MeasureUnit,
                MovementValue = dto.MovementValue
            };

            _db.PicsCalibrations.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity;
        }

        public async Task<PicsCalibration> UploadAsync(PicsCalibrationUploadDTO dto, CancellationToken ct)
        {
            if (dto.Pic1File is null || dto.Pic2File is null)
                throw new ArgumentException("Debe enviar Pic1File y Pic2File.");

            var id = Guid.NewGuid().ToString();
            var recordDir = Path.Combine(_imagesBasePath, id);
            Directory.CreateDirectory(recordDir);

            SaveFile(dto.Pic1File, recordDir, "pic1");
            SaveFile(dto.Pic2File, recordDir, "pic2");

            var baseRequestPath = "/SouerceStaticFiles/pics-calibrations/" + id;
            var pic1Url = baseRequestPath + "/pic1" + Path.GetExtension(dto.Pic1File.FileName);
            var pic2Url = baseRequestPath + "/pic2" + Path.GetExtension(dto.Pic2File.FileName);

            var entity = new PicsCalibration
            {
                PicsCalibrationId = id,
                GroupCailbrationId = dto.GroupCailbrationId,
                Pic1 = pic1Url,
                Pic2 = pic2Url,
                AxeDirectionCalibration = dto.AxeDirectionCalibration,
                Acepted = dto.Acepted ? 1 : 0,
                Dx = dto.Dx,
                Dy = dto.Dy,
                Confidence = dto.Confidence,
                MeasureUnit = dto.MeasureUnit,
                MovementValue = dto.MovementValue
            };

            _db.PicsCalibrations.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity;
        }

        public async Task UpdateAsync(string id, PicsCalibrationUpdateDTO dto, CancellationToken ct)
        {
            var existing = await _db.PicsCalibrations.FirstOrDefaultAsync(p => p.PicsCalibrationId == id, ct);
            if (existing is null) throw new KeyNotFoundException($"PicsCalibration {id} not found.");

            existing.GroupCailbrationId = dto.GroupCailbrationId;
            existing.Pic1 = dto.Pic1;
            existing.Pic2 = dto.Pic2;
            existing.AxeDirectionCalibration = dto.AxeDirectionCalibration;
            existing.Acepted = dto.Acepted;
            existing.Dx = dto.Dx;
            existing.Dy = dto.Dy;
            existing.Confidence = dto.Confidence;
            existing.MeasureUnit = dto.MeasureUnit;
            existing.MovementValue = dto.MovementValue;

            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(string id, CancellationToken ct)
        {
            var pic = await _db.PicsCalibrations.FirstOrDefaultAsync(p => p.PicsCalibrationId == id, ct);
            if (pic is null) throw new KeyNotFoundException($"PicsCalibration {id} not found.");

            _db.PicsCalibrations.Remove(pic);
            await _db.SaveChangesAsync(ct);
        }

        private static string SaveFile(IFormFile file, string recordDir, string name)
        {
            var ext = Path.GetExtension(file.FileName);
            var fullPath = Path.Combine(recordDir, name + ext);
            using var stream = System.IO.File.Create(fullPath);
            file.CopyTo(stream);
            return fullPath;
        }
    }
}
