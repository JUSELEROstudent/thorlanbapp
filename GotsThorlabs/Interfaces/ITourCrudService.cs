using TourEntity = GotsThorlabs.Database.EntityRepo.Entities.Tour;
using GotsThorlabs.Models;

namespace GotsThorlabs.Interfaces
{
    public interface ITourCrudService
    {
        /// <summary>
        /// Todos los recorridos, indicando cuáles tienen ya su stitching generado. Los
        /// más recientes primero, para que quien solo quiera el último no tenga que
        /// ordenar por su cuenta.
        /// </summary>
        Task<IEnumerable<TourResponseDTO>> GetAllAsync(CancellationToken ct);
        Task<TourEntity?> GetByIdAsync(long id, CancellationToken ct);
        Task<TourEntity> CreateAsync(TourDTO dto, CancellationToken ct);
        Task UpdateAsync(long id, TourDTO dto, CancellationToken ct);
        Task DeleteAsync(long id, CancellationToken ct);
    }
}
