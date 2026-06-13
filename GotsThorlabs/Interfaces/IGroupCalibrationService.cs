using GotsThorlabs.Database.EntityRepo.Entities;
using GotsThorlabs.Models;

namespace GotsThorlabs.Interfaces
{
    public interface IGroupCalibrationService
    {
        Task<IEnumerable<GroupCalibration>> GetAllAsync(CancellationToken ct);
        Task<GroupCalibration?> GetByIdAsync(string id, CancellationToken ct);
        Task<GroupCalibration> CreateAsync(GroupCalibrationDTO dto, CancellationToken ct);
        Task UpdateAsync(string id, GroupCalibrationDTO dto, CancellationToken ct);
        Task DeleteAsync(string id, CancellationToken ct);
    }
}
