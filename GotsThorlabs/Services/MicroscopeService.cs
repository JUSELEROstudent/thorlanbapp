using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Database.EntityRepo.Entities;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using Microsoft.EntityFrameworkCore;

namespace GotsThorlabs.Services
{
    public class MicroscopeService : IMicroscopeService
    {
        private readonly ThorlabsDbContext _db;

        public MicroscopeService(ThorlabsDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Microscope>> GetAllAsync(CancellationToken ct)
        {
            return await _db.Microscopes.AsNoTracking().ToListAsync(ct);
        }

        public async Task<Microscope?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _db.Microscopes.AsNoTracking().FirstOrDefaultAsync(m => m.MicroscopeId == id, ct);
        }

        public async Task<Microscope> CreateAsync(MicroscopeDTO dto, CancellationToken ct)
        {
            var entity = new Microscope
            {
                MicroscopeId = dto.MicroscopeId == Guid.Empty ? Guid.NewGuid() : dto.MicroscopeId,
                Name = dto.Name,
                Brand = dto.Brand,
                Site = dto.Site,
                AditionalInfo = dto.AditionalInfo
            };

            _db.Microscopes.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity;
        }

        public async Task UpdateAsync(Guid id, MicroscopeDTO dto, CancellationToken ct)
        {
            var existing = await _db.Microscopes.FirstOrDefaultAsync(m => m.MicroscopeId == id, ct);
            if (existing is null) throw new KeyNotFoundException($"Microscope {id} not found.");

            existing.Name = dto.Name;
            existing.Brand = dto.Brand;
            existing.Site = dto.Site;
            existing.AditionalInfo = dto.AditionalInfo;

            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct)
        {
            var microscope = await _db.Microscopes.FirstOrDefaultAsync(m => m.MicroscopeId == id, ct);
            if (microscope is null) throw new KeyNotFoundException($"Microscope {id} not found.");

            _db.Microscopes.Remove(microscope);
            await _db.SaveChangesAsync(ct);
        }
    }
}