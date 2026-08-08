using System;
using System.Collections.Generic;

namespace GotsThorlabs.Database.EntityRepo.Entities
{
    /// <summary>
    /// Resultado de la medición del tamaño de paso para un eje concreto.
    ///
    /// Se mide en los dos sentidos porque un actuador stick-slip suele avanzar distinto
    /// al ir que al volver. Eso no es un detalle académico: el recorrido mueve el eje Y
    /// en patrón "S", alternando el sentido en cada columna (TakeTour), de modo que una
    /// histéresis apreciable hace que columnas contiguas avancen distancias reales
    /// distintas y el error se acumule alternando.
    ///
    /// Los valores numéricos se guardan como texto con cultura invariante, igual que
    /// Dx/Dy/Confidence en picsCalibration, para que la configuración regional del
    /// equipo no corrompa el separador decimal.
    /// </summary>
    public partial class AxisStepCalibration
    {
        public AxisStepCalibration()
        {
            AxisStepMeasurements = new HashSet<AxisStepMeasurement>();
        }

        public string AxisStepCalibrationId { get; set; } = null!;

        public string MotorCalibrationId { get; set; } = null!;

        /// <summary>"x" (Channel1) o "y" (Channel2).</summary>
        public string AxisName { get; set; } = null!;

        /// <summary>Nanómetros por paso midiendo en sentido positivo. Null si no se ha medido.</summary>
        public string? StepSizeNmForward { get; set; }

        /// <summary>Nanómetros por paso midiendo en sentido negativo. Null si no se ha medido.</summary>
        public string? StepSizeNmBackward { get; set; }

        /// <summary>
        /// Valor que usa el sistema: la media de ambos sentidos. Null mientras falte
        /// alguno de los dos.
        /// </summary>
        public string? StepSizeNm { get; set; }

        /// <summary>
        /// Diferencia relativa entre sentidos, en porcentaje. Solo informa: no bloquea
        /// el recorrido, pero por encima del 10 % el patrón "S" queda en entredicho.
        /// </summary>
        public string? HysteresisPct { get; set; }

        /// <summary>
        /// Error relativo de la pendiente ajustada, en porcentaje. Sale del error
        /// estándar de la regresión por el origen sobre todas las paradas, no de
        /// promediar mediciones independientes.
        /// </summary>
        public string? RelativeErrorPct { get; set; }

        /// <summary>"in_progress" o "complete".</summary>
        public string Status { get; set; } = null!;

        public virtual MotorCalibration MotorCalibration { get; set; } = null!;

        public virtual ICollection<AxisStepMeasurement> AxisStepMeasurements { get; set; }
    }
}
