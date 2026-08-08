using System.Globalization;
using GotsThorlabs.Database.EntityRepo.Entities;

namespace GotsThorlabs.BLL
{
    /// <summary>
    /// Ajusta el tamaño real del paso del motor a partir de las lecturas del pie de rey.
    ///
    /// Se usa el mismo modelo que la calibración óptica (ver MosaicGridCalculator.FitAxis):
    /// una regresión lineal por el origen sobre TODAS las paradas de la travesía, y no el
    /// promedio de mediciones independientes. La formulación por el origen es la
    /// físicamente correcta —cero pasos deben dar cero desplazamiento— y su ponderación
    /// natural por s² hace que la parada más lejana, que es la de mejor relación
    /// señal-ruido frente a la resolución del instrumento, domine el ajuste.
    ///
    /// Con diez paradas esto da mejor precisión que una sola medida al extremo: el error
    /// del instrumento se reparte entre todos los puntos en vez de recaer sobre uno.
    /// </summary>
    public static class StepSizeCalculator
    {
        private const double NmPerMm = 1_000_000d;

        /// <summary>Resultado del ajuste de una travesía (un sentido de un eje).</summary>
        public class TraverseFit
        {
            /// <summary>Nanómetros por paso, en magnitud.</summary>
            public double StepSizeNm { get; set; }

            /// <summary>Error relativo de la pendiente, en porcentaje.</summary>
            public double RelativeErrorPct { get; set; }

            /// <summary>Paradas utilizadas (sin contar la lectura de partida).</summary>
            public int PointCount { get; set; }
        }

        /// <summary>
        /// Ajusta una travesía. Devuelve null si no hay al menos dos paradas además de la
        /// lectura de partida: con un solo punto la pendiente sale exacta por
        /// construcción y no habría forma de estimar su error.
        /// </summary>
        public static TraverseFit? FitTraverse(IEnumerable<AxisStepMeasurement> measurements)
        {
            var ordered = measurements
                .OrderBy(m => m.Sequence)
                .ToList();

            if (ordered.Count < 3) return null;

            // La parada 0 es la lectura de partida: el montaje arranca con la separación
            // ligeramente abierta para poder leerla, así que el desplazamiento de cada
            // parada se mide contra ella y no contra el cero del instrumento.
            if (!TryParse(ordered[0].CaliperReadingMm, out var baseline)) return null;

            var points = new List<(double Steps, double Nm)>();
            foreach (var m in ordered.Skip(1))
            {
                if (!TryParse(m.CaliperReadingMm, out var reading)) continue;
                if (m.StepsCommanded <= 0) continue;

                // En el sentido de retroceso la separación se cierra y la diferencia sale
                // negativa; lo que interesa es la magnitud del desplazamiento.
                var nm = Math.Abs(reading - baseline) * NmPerMm;
                points.Add((m.StepsCommanded, nm));
            }

            if (points.Count < 2) return null;

            var sumS2 = points.Sum(p => p.Steps * p.Steps);
            if (sumS2 <= 0) return null;

            var slope = points.Sum(p => p.Steps * p.Nm) / sumS2;
            if (slope <= 0) return null;

            // Error estándar de la pendiente en una regresión por el origen.
            var residual = points.Sum(p => Math.Pow(p.Nm - slope * p.Steps, 2));
            var sigma = Math.Sqrt(residual / Math.Max(points.Count - 1, 1));
            var standardError = sigma / Math.Sqrt(sumS2);

            return new TraverseFit
            {
                StepSizeNm = slope,
                RelativeErrorPct = slope > 0 ? 100d * standardError / slope : 0d,
                PointCount = points.Count
            };
        }

        /// <summary>
        /// Combina los dos sentidos de un eje. El valor que usa el sistema es la media,
        /// y la histéresis se reporta como la diferencia relativa entre sentidos.
        ///
        /// La histéresis no bloquea nada, pero conviene mirarla: el recorrido mueve el eje
        /// Y alternando el sentido en cada columna (patrón "S" en TakeTour), de modo que
        /// una diferencia apreciable entre ida y vuelta se traduce en columnas contiguas
        /// que avanzan distancias reales distintas.
        /// </summary>
        public static void Combine(AxisStepCalibration axis, TraverseFit? forward, TraverseFit? backward)
        {
            axis.StepSizeNmForward = forward != null ? Format(forward.StepSizeNm) : null;
            axis.StepSizeNmBackward = backward != null ? Format(backward.StepSizeNm) : null;

            if (forward == null || backward == null)
            {
                axis.StepSizeNm = null;
                axis.HysteresisPct = null;
                axis.RelativeErrorPct = forward?.RelativeErrorPct is double f ? Format(f)
                                      : backward?.RelativeErrorPct is double b ? Format(b)
                                      : null;
                axis.Status = "in_progress";
                return;
            }

            var mean = (forward.StepSizeNm + backward.StepSizeNm) / 2d;
            axis.StepSizeNm = Format(mean);
            axis.HysteresisPct = Format(mean > 0
                ? 100d * Math.Abs(forward.StepSizeNm - backward.StepSizeNm) / mean
                : 0d);

            // Se reporta el peor de los dos sentidos: es la cota honesta del error.
            axis.RelativeErrorPct = Format(Math.Max(forward.RelativeErrorPct, backward.RelativeErrorPct));
            axis.Status = "complete";
        }

        /// <summary>
        /// Parseo tolerante a la cultura: se intenta primero la invariante, que es como se
        /// persiste, y luego la del sistema, para que un equipo configurado con coma
        /// decimal no rompa la lectura.
        /// </summary>
        public static bool TryParse(string? raw, out double value)
        {
            value = 0d;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                || double.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        public static string Format(double value) => value.ToString("G", CultureInfo.InvariantCulture);
    }
}
