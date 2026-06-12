using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using Microsoft.EntityFrameworkCore;
using TourEntity = GotsThorlabs.Database.EntityRepo.Entities.Tour;

namespace GotsThorlabs.Services
{
    public class TourCrudService : ITourCrudService
    {
        private readonly ThorlabsDbContext _db;

        public TourCrudService(ThorlabsDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<TourEntity>> GetAllAsync(CancellationToken ct)
        {
            return await _db.Tours.AsNoTracking().ToListAsync(ct);
        }

        public async Task<TourEntity?> GetByIdAsync(int id, CancellationToken ct)
        {
            return await _db.Tours.AsNoTracking().FirstOrDefaultAsync(t => t.IdTour == id, ct);
        }

        public async Task<TourEntity> CreateAsync(TourDTO dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.NameFolder))
                throw new ArgumentException("El campo NameFolder es requerido.");

            var entity = new TourEntity
            {
                Date = dto.Date == default ? DateTime.UtcNow : dto.Date,
                NameFolder = dto.NameFolder,
                NumberX = dto.NumberX,
                NumberY = dto.NumberY,
                NumberZ = dto.NumberZ,
                Camera = dto.Camera,
                EndStatus = dto.EndStatus,
                PicsCalibrationId = dto.PicsCalibrationId
            };

            _db.Tours.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity;
        }

        public async Task UpdateAsync(int id, TourDTO dto, CancellationToken ct)
        {
            var existing = await _db.Tours.FirstOrDefaultAsync(t => t.IdTour == id, ct);
            if (existing is null) throw new KeyNotFoundException($"Tour {id} not found.");

            existing.Date = dto.Date == default ? existing.Date : dto.Date;
            existing.NameFolder = dto.NameFolder;
            existing.NumberX = dto.NumberX;
            existing.NumberY = dto.NumberY;
            existing.NumberZ = dto.NumberZ;
            existing.Camera = dto.Camera;
            existing.EndStatus = dto.EndStatus;

            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct)
        {
            var tour = await _db.Tours.FirstOrDefaultAsync(t => t.IdTour == id, ct);
            if (tour is null) throw new KeyNotFoundException($"Tour {id} not found.");

            _db.Tours.Remove(tour);
            await _db.SaveChangesAsync(ct);
        }
    }
}