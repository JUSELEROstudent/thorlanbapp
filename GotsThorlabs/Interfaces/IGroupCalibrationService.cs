using GotsThorlabs.Database.EntityRepo.Entities;
using GotsThorlabs.Models;

namespace GotsThorlabs.Interfaces
{
    public interface IGroupCalibrationService
    {
        Task<IEnumerable<GroupCalibration>> GetAllAsync(CancellationToken ct);
        Task<GroupCalibration?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<GroupCalibration> CreateAsync(GroupCalibrationDTO dto, CancellationToken ct);
        Task UpdateAsync(Guid id, GroupCalibrationDTO dto, CancellationToken ct);
        Task DeleteAsync(Guid id, CancellationToken ct);
    }
}