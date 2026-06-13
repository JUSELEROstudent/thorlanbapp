using GotsThorlabs.Database.EntityRepo.Entities;
using GotsThorlabs.Models;

namespace GotsThorlabs.Interfaces
{
    public interface IIncreaseService
    {
        Task<IEnumerable<Increase>> GetAllAsync(CancellationToken ct);
        Task<Increase?> GetByIdAsync(string id, CancellationToken ct);
        Task<Increase> CreateAsync(IncreaseDTO dto, CancellationToken ct);
        Task UpdateAsync(string id, IncreaseDTO dto, CancellationToken ct);
        Task DeleteAsync(string id, CancellationToken ct);
    }
}
