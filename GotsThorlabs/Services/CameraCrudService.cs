using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Database.EntityRepo.Entities;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using Microsoft.EntityFrameworkCore;

namespace GotsThorlabs.Services
{
    public class CameraCrudService : ICameraCrudService
    {
        private readonly ThorlabsDbContext _db;
        private readonly CameraServiceFactory _cameraFactory;

        public CameraCrudService(ThorlabsDbContext db, CameraServiceFactory cameraFactory)
        {
            _db = db;
            _cameraFactory = cameraFactory;
        }

        public async Task<IEnumerable<Camera>> GetAllAsync(CancellationToken ct)
        {
            return await _db.Cameras.AsNoTracking().ToListAsync(ct);
        }

        public async Task<Camera?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _db.Cameras.AsNoTracking().FirstOrDefaultAsync(c => c.CameraId == id, ct);
        }

        public async Task<Camera> CreateAsync(CameraDTO dto, CancellationToken ct)
        {
            var driverType = string.IsNullOrWhiteSpace(dto.DriverType) ? "generic" : dto.DriverType.Trim().ToLowerInvariant();

            var entity = new Camera
            {
                CameraId = dto.CameraId == Guid.Empty ? Guid.NewGuid() : dto.CameraId,
                Name = dto.Name,
                LocalIdentifier = dto.LocalIdentifier,
                Features = dto.Features,
                DriverType = driverType
            };

            if (!string.IsNullOrWhiteSpace(entity.LocalIdentifier))
            {
                var resolved = _cameraFactory.ResolveLocalIdentifier(driverType, entity.LocalIdentifier);
                if (resolved is not null)
                    entity.LocalIdentifier = resolved;
            }

            var cameraExist = await _db.Cameras.AnyAsync(item => item.Name.Trim().ToLower() == entity.Name.Trim().ToLower(), ct);
            if (cameraExist)
                throw new InvalidOperationException("Ya existe una cámara con ese nombre.");

            _db.Cameras.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity;
        }

        public async Task UpdateAsync(Guid id, CameraDTO dto, CancellationToken ct)
        {
            var existing = await _db.Cameras.FirstOrDefaultAsync(c => c.CameraId == id, ct);
            if (existing is null) throw new KeyNotFoundException($"Camera {id} not found.");

            existing.Name = dto.Name;
            existing.LocalIdentifier = dto.LocalIdentifier;
            existing.Features = dto.Features;
            existing.DriverType = string.IsNullOrWhiteSpace(dto.DriverType) ? "generic" : dto.DriverType.Trim().ToLowerInvariant();

            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct)
        {
            var camera = await _db.Cameras.FirstOrDefaultAsync(c => c.CameraId == id, ct);
            if (camera is null) throw new KeyNotFoundException($"Camera {id} not found.");

            _db.Cameras.Remove(camera);
            await _db.SaveChangesAsync(ct);
        }
    }
}