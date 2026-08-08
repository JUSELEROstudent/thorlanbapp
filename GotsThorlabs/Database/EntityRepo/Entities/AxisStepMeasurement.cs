using System;
using System.Collections.Generic;

namespace GotsThorlabs.Database.EntityRepo.Entities
{
    /// <summary>
    /// Una parada del recorrido de medición: cuántos pasos acumulados llevaba el motor y
    /// qué leyó el operador en el pie de rey.
    ///
    /// Se guarda la lectura CRUDA del instrumento y no el desplazamiento ya restado. Así
    /// la medición queda auditable, y si el operador se equivoca en una parada puede
    /// corregir esa sola lectura y recalcular, en vez de repetir la travesía completa.
    ///
    /// Cada fila se persiste en cuanto se introduce: es lo que permite interrumpir la
    /// medición —que dura unos veinte minutos por eje— y retomarla después.
    /// </summary>
    public partial class AxisStepMeasurement
    {
        public string AxisStepMeasurementId { get; set; } = null!;

        public string AxisStepCalibrationId { get; set; } = null!;

        /// <summary>"forward" o "backward".</summary>
        public string Direction { get; set; } = null!;

        /// <summary>Orden de la parada dentro de la travesía. 0 es la lectura de partida.</summary>
        public long Sequence { get; set; }

        /// <summary>
        /// Pasos acumulados desde el inicio de la travesía. Las paradas se definen en
        /// pasos y no en milímetros para que cada tramo tarde lo mismo con independencia
        /// del tamaño real del paso, que es justamente lo que se está midiendo.
        /// </summary>
        public long StepsCommanded { get; set; }

        /// <summary>
        /// Lectura del pie de rey en milímetros, en cultura invariante. El montaje parte
        /// con la separación ligeramente abierta para poder tomar la lectura inicial; el
        /// desplazamiento de cada parada es esta lectura menos la de la parada 0.
        /// </summary>
        public string CaliperReadingMm { get; set; } = null!;

        public string MeasuredAt { get; set; } = null!;

        public virtual AxisStepCalibration AxisStepCalibration { get; set; } = null!;
    }
}
