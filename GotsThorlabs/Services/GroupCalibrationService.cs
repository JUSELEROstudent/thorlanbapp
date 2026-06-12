using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Database.EntityRepo.Entities;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using Microsoft.EntityFrameworkCore;

namespace GotsThorlabs.Services
{
    public class GroupCalibrationService : IGroupCalibrationService
    {
        private readonly ThorlabsDbContext _db;

        public GroupCalibrationService(ThorlabsDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<GroupCalibration>> GetAllAsync(CancellationToken ct)
        {
            return await _db.GroupCalibrations.AsNoTracking().ToListAsync(ct);
        }

        public async Task<GroupCalibration?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _db.GroupCalibrations.AsNoTracking().FirstOrDefaultAsync(g => g.GroupCailbrationId == id, ct);
        }

        public async Task<GroupCalibration> CreateAsync(GroupCalibrationDTO dto, CancellationToken ct)
        {
            var entity = new GroupCalibration
            {
                GroupCailbrationId = dto.GroupCailbrationId == Guid.Empty ? Guid.NewGuid() : dto.GroupCailbrationId,
                CameraId = dto.CameraId,
                MicroscopeId = dto.MicroscopeId,
                IncreaseId = dto.IncreaseId,
                Date = dto.Date,
                AditionalInfo = dto.AditionalInfo
            };

            _db.GroupCalibrations.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity;
        }

        public async Task UpdateAsync(Guid id, GroupCalibrationDTO dto, CancellationToken ct)
        {
            var existing = await _db.GroupCalibrations.FirstOrDefaultAsync(g => g.GroupCailbrationId == id, ct);
            if (existing is null) throw new KeyNotFoundException($"GroupCalibration {id} not found.");

            existing.CameraId = dto.CameraId;
            existing.MicroscopeId = dto.MicroscopeId;
            existing.IncreaseId = dto.IncreaseId;
            existing.Date = dto.Date;
            existing.AditionalInfo = dto.AditionalInfo;

            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct)
        {
            var group = await _db.GroupCalibrations.FirstOrDefaultAsync(g => g.GroupCailbrationId == id, ct);
            if (group is null) throw new KeyNotFoundException($"GroupCalibration {id} not found.");

            _db.GroupCalibrations.Remove(group);
            await _db.SaveChangesAsync(ct);
        }
    }
}