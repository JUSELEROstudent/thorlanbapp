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

        /// <summary>
        /// Parámetros que acepta el dispositivo de esta cámara (consultados al
        /// hardware) junto con los que el usuario dejó guardados.
        /// </summary>
        Task<CameraParametersResponseDTO> GetParametersAsync(string id, CancellationToken ct);

        /// <summary>
        /// Guarda los parámetros de captura de una cámara en settingsJson y avisa al
        /// driver para que los tome en la próxima captura.
        /// </summary>
        Task UpdateParametersAsync(string id, CameraParametersUpdateDTO dto, CancellationToken ct);
    }
}
