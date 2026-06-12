using GotsThorlabs.Database.EntityRepo.Entities;
using GotsThorlabs.Models;

namespace GotsThorlabs.Interfaces
{
    public interface IMicroscopeService
    {
        Task<IEnumerable<Microscope>> GetAllAsync(CancellationToken ct);
        Task<Microscope?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<Microscope> CreateAsync(MicroscopeDTO dto, CancellationToken ct);
        Task UpdateAsync(Guid id, MicroscopeDTO dto, CancellationToken ct);
        Task DeleteAsync(Guid id, CancellationToken ct);
    }
}