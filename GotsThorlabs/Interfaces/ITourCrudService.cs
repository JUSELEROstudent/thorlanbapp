using TourEntity = GotsThorlabs.Database.EntityRepo.Entities.Tour;
using GotsThorlabs.Models;

namespace GotsThorlabs.Interfaces
{
    public interface ITourCrudService
    {
        Task<IEnumerable<TourEntity>> GetAllAsync(CancellationToken ct);
        Task<TourEntity?> GetByIdAsync(long id, CancellationToken ct);
        Task<TourEntity> CreateAsync(TourDTO dto, CancellationToken ct);
        Task UpdateAsync(long id, TourDTO dto, CancellationToken ct);
        Task DeleteAsync(long id, CancellationToken ct);
    }
}
