using GotsThorlabs.Database.EntityRepo.Entities;
using GotsThorlabs.Models;

namespace GotsThorlabs.Interfaces
{
    public interface ICameraCrudService
    {
        Task<IEnumerable<Camera>> GetAllAsync(CancellationToken ct);
        Task<Camera?> GetByIdAsync(string id, CancellationToken ct);
        Task<Camera> CreateAsync(CameraDTO dto, CancellationToken ct);
        Task UpdateAsync(string id, CameraDTO dto, CancellationToken ct);
        Task DeleteAsync(string id, CancellationToken ct);
    }
}
