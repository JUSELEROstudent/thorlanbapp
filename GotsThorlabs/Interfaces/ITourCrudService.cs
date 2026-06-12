using TourEntity = GotsThorlabs.Database.EntityRepo.Entities.Tour;
using GotsThorlabs.Models;

namespace GotsThorlabs.Interfaces
{
    public interface ITourCrudService
    {
        Task<IEnumerable<TourEntity>> GetAllAsync(CancellationToken ct);
        Task<TourEntity?> GetByIdAsync(int id, CancellationToken ct);
        Task<TourEntity> CreateAsync(TourDTO dto, CancellationToken ct);
        Task UpdateAsync(int id, TourDTO dto, CancellationToken ct);
        Task DeleteAsync(int id, CancellationToken ct);
    }
}