using GotsThorlabs.Database.EntityRepo.Entities;
using GotsThorlabs.Models;

namespace GotsThorlabs.Interfaces
{
    public interface IMicroscopeService
    {
        Task<IEnumerable<Microscope>> GetAllAsync(CancellationToken ct);
        Task<Microscope?> GetByIdAsync(string id, CancellationToken ct);
        Task<Microscope> CreateAsync(MicroscopeDTO dto, CancellationToken ct);
        Task UpdateAsync(string id, MicroscopeDTO dto, CancellationToken ct);
        Task DeleteAsync(string id, CancellationToken ct);
    }
}
