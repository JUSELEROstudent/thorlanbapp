using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Database.EntityRepo.Entities;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using Microsoft.EntityFrameworkCore;

namespace GotsThorlabs.Services
{
    public class IncreaseService : IIncreaseService
    {
        private readonly ThorlabsDbContext _db;

        public IncreaseService(ThorlabsDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Increase>> GetAllAsync(CancellationToken ct)
        {
            return await _db.Increases.AsNoTracking().ToListAsync(ct);
        }

        public async Task<Increase?> GetByIdAsync(string id, CancellationToken ct)
        {
            return await _db.Increases.AsNoTracking().FirstOrDefaultAsync(i => i.IncreaseId == id, ct);
        }

        public async Task<Increase> CreateAsync(IncreaseDTO dto, CancellationToken ct)
        {
            var entity = new Increase
            {
                IncreaseId = string.IsNullOrWhiteSpace(dto.IncreaseId) ? Guid.NewGuid().ToString() : dto.IncreaseId,
                Name = dto.Name,
                Value = dto.Value,
                AditionalInfo = dto.AditionalInfo
            };

            _db.Increases.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity;
        }

        public async Task UpdateAsync(string id, IncreaseDTO dto, CancellationToken ct)
        {
            var existing = await _db.Increases.FirstOrDefaultAsync(i => i.IncreaseId == id, ct);
            if (existing is null) throw new KeyNotFoundException($"Increase {id} not found.");

            existing.Name = dto.Name;
            existing.Value = dto.Value;
            existing.AditionalInfo = dto.AditionalInfo;

            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(string id, CancellationToken ct)
        {
            var increase = await _db.Increases.FirstOrDefaultAsync(i => i.IncreaseId == id, ct);
            if (increase is null) throw new KeyNotFoundException($"Increase {id} not found.");

            _db.Increases.Remove(increase);
            await _db.SaveChangesAsync(ct);
        }
    }
}
