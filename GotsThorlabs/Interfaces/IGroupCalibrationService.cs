using GotsThorlabs.Database.EntityRepo.Entities;
using GotsThorlabs.Models;

namespace GotsThorlabs.Interfaces
{
    public interface IGroupCalibrationService
    {
        /// <summary>
        /// Todos los grupos de calibración con el nombre de la cámara, el microscopio y
        /// el objetivo ya resueltos, para que la vista no tenga que cruzar identificadores
        /// contra listas que pide por separado.
        /// </summary>
        Task<IEnumerable<GroupCalibrationResponseDTO>> GetAllAsync(CancellationToken ct);

        /// <summary>
        /// Todos los grupos de calibración con la cámara, el microscopio y el objetivo
        /// que los identifican, más sus mediciones. Alimenta la vista de calibraciones,
        /// que necesita mostrar a qué combinación de equipos pertenece cada medición
        /// sin tener que hacer una petición por grupo.
        /// </summary>
        Task<IEnumerable<GroupCalibration>> GetAllWithDetailsAsync(CancellationToken ct);

        Task<GroupCalibrationResponseDTO?> GetByIdAsync(string id, CancellationToken ct);
        Task<GroupCalibration> CreateAsync(GroupCalibrationDTO dto, CancellationToken ct);
        Task UpdateAsync(string id, GroupCalibrationDTO dto, CancellationToken ct);
        Task DeleteAsync(string id, CancellationToken ct);
    }
}
