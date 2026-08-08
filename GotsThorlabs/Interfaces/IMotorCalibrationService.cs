using GotsThorlabs.Models;

namespace GotsThorlabs.Interfaces
{
    /// <summary>
    /// Caracterización mecánica del motor: cuánto mide realmente cada paso en cada eje.
    ///
    /// La medición es manual (pie de rey) y dura unos veinte minutos por eje, así que el
    /// servicio está diseñado para ser interrumpible: cada lectura se persiste en cuanto
    /// se registra y el estado vive en la base, no en el cliente.
    /// </summary>
    public interface IMotorCalibrationService
    {
        /// <summary>Caracterizaciones de un grupo, la vigente primero.</summary>
        Task<IEnumerable<MotorCalibrationDTO>> GetByGroupAsync(string groupCalibrationId, CancellationToken ct);

        Task<MotorCalibrationDTO?> GetByIdAsync(string id, CancellationToken ct);

        /// <summary>
        /// Inicia una caracterización con sus dos ejes en progreso. No mueve el motor.
        /// </summary>
        Task<MotorCalibrationDTO> CreateAsync(MotorCalibrationCreateDTO dto, CancellationToken ct);

        /// <summary>
        /// Registra la lectura del pie de rey de una parada y recalcula el ajuste del eje.
        /// Si ya existía una lectura para esa misma parada y sentido, la reemplaza: repetir
        /// una parada es una corrección, no una medición nueva.
        /// </summary>
        Task<AxisStepCalibrationDTO> RecordMeasurementAsync(
            string motorCalibrationId, AxisStepMeasurementCreateDTO dto, CancellationToken ct);

        /// <summary>Corrige una lectura ya registrada y recalcula el eje.</summary>
        Task<AxisStepCalibrationDTO> UpdateMeasurementAsync(
            string measurementId, AxisStepMeasurementUpdateDTO dto, CancellationToken ct);

        Task<AxisStepCalibrationDTO> DeleteMeasurementAsync(string measurementId, CancellationToken ct);

        /// <summary>
        /// Mueve un eje a una posición absoluta usando los parámetros de accionamiento de
        /// esta caracterización. Es el mismo motor y los mismos parámetros que se están
        /// midiendo, por lo que el movimiento no puede delegarse en el endpoint genérico.
        /// </summary>
        Task MoveAsync(string motorCalibrationId, MotorMoveDTO dto, CancellationToken ct);

        /// <summary>Marca la caracterización como vigente para su grupo.</summary>
        Task AcceptAsync(string motorCalibrationId, CancellationToken ct);

        /// <summary>
        /// Copia las mediciones de otra caracterización a este grupo. Evita repetir media
        /// hora de pie de rey cuando el montaje mecánico no cambió y solo se creó un combo
        /// óptico nuevo (otro objetivo sobre la misma platina).
        /// </summary>
        Task<MotorCalibrationDTO> CopyToGroupAsync(
            string sourceMotorCalibrationId, string targetGroupCalibrationId, CancellationToken ct);

        Task DeleteAsync(string motorCalibrationId, CancellationToken ct);
    }
}
