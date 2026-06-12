using GotsThorlabs.Database.EntityRepo.Entities;
using GotsThorlabs.Models;

namespace GotsThorlabs.Interfaces
{
    public interface ICameraCrudService
    {
        Task<IEnumerable<Camera>> GetAllAsync(CancellationToken ct);
        Task<Camera?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<Camera> CreateAsync(CameraDTO dto, CancellationToken ct);
        Task UpdateAsync(Guid id, CameraDTO dto, CancellationToken ct);
        Task DeleteAsync(Guid id, CancellationToken ct);
    }
}