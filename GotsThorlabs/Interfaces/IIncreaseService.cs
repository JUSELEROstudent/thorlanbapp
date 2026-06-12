using GotsThorlabs.Database.EntityRepo.Entities;
using GotsThorlabs.Models;

namespace GotsThorlabs.Interfaces
{
    public interface IIncreaseService
    {
        Task<IEnumerable<Increase>> GetAllAsync(CancellationToken ct);
        Task<Increase?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<Increase> CreateAsync(IncreaseDTO dto, CancellationToken ct);
        Task UpdateAsync(Guid id, IncreaseDTO dto, CancellationToken ct);
        Task DeleteAsync(Guid id, CancellationToken ct);
    }
}