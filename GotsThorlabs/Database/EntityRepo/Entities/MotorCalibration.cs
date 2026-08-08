using System;
using System.Collections.Generic;

namespace GotsThorlabs.Database.EntityRepo.Entities
{
    /// <summary>
    /// Caracterización mecánica del motor para un grupo de calibración: con qué
    /// parámetros de accionamiento se movió y cuánto mide realmente cada paso.
    ///
    /// Existe porque el tamaño físico del paso NO es un dato del fabricante. Un
    /// actuador piezoeléctrico inercial avanza por stick-slip, de modo que el paso real
    /// depende de la velocidad, la aceleración y la carga sobre la platina. Hasta ahora
    /// el sistema asumía un nominal de 30 nm fijado en código
    /// (MosaicGridCalculator.NmPerStep), sin forma de verificarlo.
    ///
    /// Guardar StepRate y StepAcceleration aquí resuelve además un problema aparte: la
    /// calibración óptica y el recorrido los tenían replicados como literales en dos
    /// archivos distintos, y si divergían la relación píxeles/paso quedaba medida para
    /// un comportamiento del motor diferente del que se usaba después, sin ninguna
    /// manifestación visible.
    /// </summary>
    public partial class MotorCalibration
    {
        public MotorCalibration()
        {
            AxisStepCalibrations = new HashSet<AxisStepCalibration>();
        }

        public string MotorCalibrationId { get; set; } = null!;

        /// <summary>Grupo de calibración al que pertenece esta caracterización.</summary>
        public string GroupCailbrationId { get; set; } = null!;

        /// <summary>Serial del controlador KIM101 con el que se midió.</summary>
        public string KimDeviceId { get; set; } = null!;

        /// <summary>
        /// Parámetros de accionamiento vigentes durante la medición. Son los que deben
        /// aplicarse también al calibrar ópticamente y al ejecutar el recorrido: medir el
        /// paso a una velocidad y moverse a otra invalida el resultado.
        /// </summary>
        public long StepRate { get; set; }

        public long StepAcceleration { get; set; }

        /// <summary>
        /// "in_progress" mientras faltan lecturas, "complete" cuando ambos ejes tienen
        /// sus dos sentidos medidos. La medición con pie de rey puede tomar media hora y
        /// no siempre se termina de una sentada, así que el estado vive en la base y no
        /// en el navegador: si el operador la interrumpe, retoma donde quedó.
        /// </summary>
        public string Status { get; set; } = null!;

        /// <summary>
        /// 1 en la caracterización que se usa. Permite volver a medir conservando el
        /// histórico en vez de sobrescribirlo, igual que se hace con picsCalibration.
        /// </summary>
        public long Acepted { get; set; }

        public string Date { get; set; } = null!;

        public string? AditionalInfo { get; set; }

        public virtual GroupCalibration GroupCailbration { get; set; } = null!;

        public virtual ICollection<AxisStepCalibration> AxisStepCalibrations { get; set; }
    }
}
