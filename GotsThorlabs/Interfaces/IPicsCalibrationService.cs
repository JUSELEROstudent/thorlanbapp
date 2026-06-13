using GotsThorlabs.Database.EntityRepo.Entities;
using GotsThorlabs.Models;

namespace GotsThorlabs.Interfaces
{
    public interface IPicsCalibrationService
    {
        Task<IEnumerable<PicsCalibration>> GetAllAsync(CancellationToken ct);
        Task<PicsCalibration?> GetByIdAsync(string id, CancellationToken ct);
        Task<PicsCalibration> CreateAsync(PicsCalibrationUploadDTO dto, CancellationToken ct);
        Task<PicsCalibration> UploadAsync(PicsCalibrationUploadDTO dto, CancellationToken ct);
        Task UpdateAsync(string id, PicsCalibrationUpdateDTO dto, CancellationToken ct);
        Task DeleteAsync(string id, CancellationToken ct);
    }
}
