namespace GotsThorlabs.BLL
{
    /// <summary>
    /// Estimación del tiempo que tardará un recorrido, a partir del grid ya calculado.
    ///
    /// Se separa del cálculo del grid porque el tiempo depende de cosas que el grid no
    /// conoce: la velocidad configurada del motor y el patrón de barrido. El grid decide
    /// cuántas imágenes y cuántos pasos entre ellas; esto traduce eso a segundos.
    ///
    /// En este montaje el tiempo lo domina el motor, no la cámara: un movimiento entre
    /// filas son decenas de miles de pasos a unos cientos de pasos por segundo, mientras
    /// que la captura ronda el medio segundo. Por eso el desglose se devuelve separado,
    /// para que se vea dónde se va el tiempo antes de lanzar un recorrido de una hora.
    /// </summary>
    public static class TourTimeEstimator
    {
        /// <summary>
        /// Espera tras alcanzar la posición para que amortigüe la vibración residual.
        /// Es la misma constante que aplica Move_Method1 tras cada movimiento.
        /// </summary>
        public const double SettleSeconds = 0.5;

        /// <summary>
        /// Coste aproximado de capturar: obtener el cuadro, medir nitidez, guardarlo en
        /// disco, registrar la fila y copiarlo al mosaico. Es empírico y por eso se
        /// reporta aparte del tiempo de motor, que es el que domina.
        /// </summary>
        public const double CaptureSeconds = 0.6;

        public class Estimate
        {
            public int ImagesX { get; set; }
            public int ImagesY { get; set; }
            public int TotalImages { get; set; }

            public double MotionSeconds { get; set; }
            public double CaptureSeconds { get; set; }
            public double TotalSeconds { get; set; }

            public int MotorStepX { get; set; }
            public int MotorStepY { get; set; }
            public int MotorStepYBackward { get; set; }
            public long StepRate { get; set; }

            public string Pattern { get; set; } = default!;

            /// <summary>Área que el recorrido cubrirá realmente, en milímetros.</summary>
            public double CoveredXmm { get; set; }
            public double CoveredYmm { get; set; }

            /// <summary>False si algún eje cayó al paso nominal por falta de medición.</summary>
            public bool StepSizeMeasured { get; set; }

            public List<string> Warnings { get; set; } = new();
        }

        public static Estimate Calculate(
            MosaicGridCalculator.GridResult grid,
            SweepPattern pattern,
            long stepRate)
        {
            var rate = Math.Max(stepRate, 1);
            double MoveSeconds(long steps) => Math.Abs(steps) / (double)rate + SettleSeconds;

            // La primera fila de cada columna ya está en posición (es donde terminó el
            // movimiento anterior), así que solo se pagan ImagesY-1 desplazamientos.
            int movesPerColumn = Math.Max(grid.ImagesY - 1, 0);

            int oddColumns = grid.ImagesX / 2;
            int evenColumns = grid.ImagesX - oddColumns;

            // En la serpentina escalada las columnas de vuelta usan más pasos para cubrir
            // la misma distancia; en las otras dos, ambos sentidos usan los mismos.
            int stepOdd = pattern == SweepPattern.SerpentineScaled
                ? grid.MotorStepYBackward
                : grid.MotorStepY;

            double motion;
            if (pattern == SweepPattern.Unidirectional)
            {
                // Todas las columnas suben con el mismo paso, y entre columnas se vuelve
                // al inicio con un movimiento compensado que no produce ninguna imagen.
                motion = grid.ImagesX * movesPerColumn * MoveSeconds(grid.MotorStepY);
                motion += Math.Max(grid.ImagesX - 1, 0)
                          * MoveSeconds((long)movesPerColumn * grid.MotorStepYBackward);
            }
            else
            {
                motion = evenColumns * movesPerColumn * MoveSeconds(grid.MotorStepY);
                motion += oddColumns * movesPerColumn * MoveSeconds(stepOdd);
            }

            // El primer avance en X es a la posición 0, donde ya está: ImagesX-1 movimientos.
            motion += Math.Max(grid.ImagesX - 1, 0) * MoveSeconds(grid.MotorStepX);

            int total = grid.ImagesX * grid.ImagesY;
            double capture = total * CaptureSeconds;

            var estimate = new Estimate
            {
                ImagesX = grid.ImagesX,
                ImagesY = grid.ImagesY,
                TotalImages = total,
                MotionSeconds = motion,
                CaptureSeconds = capture,
                TotalSeconds = motion + capture,
                MotorStepX = grid.MotorStepX,
                MotorStepY = grid.MotorStepY,
                MotorStepYBackward = grid.MotorStepYBackward,
                StepRate = rate,
                Pattern = pattern.ToString(),
                CoveredXmm = Math.Max(grid.ImagesX - 1, 0) * grid.StepMmX,
                CoveredYmm = Math.Max(grid.ImagesY - 1, 0) * grid.StepMmY,
                StepSizeMeasured = grid.StepSizeMeasured
            };

            if (!grid.StepSizeMeasured)
            {
                estimate.Warnings.Add(
                    "El tamaño de paso no está verificado en algún eje: se usa el nominal de 30 nm. " +
                    "El número de imágenes —y por tanto el tiempo y el área cubierta— es una estimación gruesa.");
            }

            if (pattern == SweepPattern.SerpentineScaled && !grid.BackwardStepMeasured)
            {
                estimate.Warnings.Add(
                    "Se pidió serpentina compensada pero el eje Y no tiene medido el sentido de vuelta. " +
                    "El recorrido se comportará como serpentina simple.");
            }

            if (estimate.TotalSeconds > 3600)
            {
                estimate.Warnings.Add(
                    $"El recorrido supera la hora ({estimate.TotalSeconds / 3600:F1} h). " +
                    "Considere reducir el área o subir StepRate.");
            }

            return estimate;
        }
    }
}
