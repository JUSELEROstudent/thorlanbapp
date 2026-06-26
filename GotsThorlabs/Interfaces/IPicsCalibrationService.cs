using GotsThorlabs.Database.EntityRepo.Entities;
using GotsThorlabs.Models;

namespace GotsThorlabs.Interfaces
{
    public interface IPicsCalibrationService
    {
        Task<IEnumerable<PicsCalibration>> GetAllAsync(CancellationToken ct);
        Task<IEnumerable<PicsCalibration>> GetByGroupCalibrationIdAsync(string groupCalibrationId, CancellationToken ct);
        Task<PicsCalibration?> GetByIdAsync(string id, CancellationToken ct);
        Task<PicsCalibration> CreateAsync(PicsCalibrationUploadDTO dto, CancellationToken ct);
        Task<PicsCalibration> UploadAsync(PicsCalibrationUploadDTO dto, CancellationToken ct);
        Task UpdateAsync(string id, PicsCalibrationUpdateDTO dto, CancellationToken ct);
        Task DeleteAsync(string id, CancellationToken ct);
        /// <summary>
        /// Ejecuta la calibración automática para un dispositivo KIM, utilizando el servicio de cámara y el servicio de correlación de fase para obtener las imágenes y calcular los desplazamientos necesarios para la calibración.
        /// </summary>
        /// <param name="kimDeviceId">device kim que se va a usar </param>
        /// <param name="groupCalibrationId">id del grupo de calibracion a usar </param>
        /// <param name="axis">eje de calibracion para el elemento automatico</param>
        /// <param name="cameraService">servicio de cámara a utilizar</param>
        /// <param name="localIdentifier">identificador local de la camara pero no debe ser  usados se debe incluir desde la camara </param>
        /// <param name="phaseCorrelationService">servicio de correlación de fase a utilizar</param>
        /// <param name="ct">token de cancelación</param>
        /// <returns>lista de calibraciones realizadas</returns>
        Task<List<PicsCalibration>> RunAutoCalibrationAsync(
            string kimDeviceId,
            string groupCalibrationId,
            string axis,
            ICameraService cameraService,
            string? localIdentifier,
            IPhaseCorrelationService phaseCorrelationService,
            CancellationToken ct);
    }
}
