using GotsThorlabs.Database.EntityRepo.Entities;

namespace GotsThorlabs.BLL
{
    /// <summary>
    /// Calcula los parámetros del grid de mosaico basándose en:
    /// - El tamaño del área a rastrear (mm)
    /// - Los registros de calibración (PicsCalibration) del grupo
    /// - La resolución de la cámara
    /// Devuelve cuántas imágenes tomar, el spacing del motor entre ellas
    /// y la calibración seleccionada para cada eje.
    /// </summary>
    public static class MosaicGridCalculator
    {
        /// <summary>
        /// Tamaño físico de cada step del motor en nanómetros.
        /// El KIM101 permite configurar el jog entre 10 y 30 nm por step.
        /// Este valor debe coincidir con el configurado en el dispositivo
        /// mediante jogParams.StepSize. Fácilmente identificable para cambiarlo.
        /// </summary>
        public const double NmPerStep = 30.0; // 30 nm = 0.00003 mm por step

        /// <summary>
        /// Fracción de la imagen que se superpone con la siguiente (70%).
        /// El avance entre imágenes es el 30% del tamaño de la imagen.
        /// </summary>
        public const double OverlapRatio = 0.70;

        /// <summary>
        /// Resultado del cálculo del grid de mosaico.
        /// </summary>
        public class GridResult
        {
            /// <summary>Número de imágenes a tomar en el eje X (columnas).</summary>
            public int ImagesX { get; set; }

            /// <summary>Número de imágenes a tomar en el eje Y (filas).</summary>
            public int ImagesY { get; set; }

            /// <summary>Pixeles de la cámara que equivalen a 1 mm en el eje X.</summary>
            public double PixelsPerMmX { get; set; }

            /// <summary>Pixeles de la cámara que equivalen a 1 mm en el eje Y.</summary>
            public double PixelsPerMmY { get; set; }

            /// <summary>Milímetros de la muestra que cubre una imagen en X.</summary>
            public double MmPerImageX { get; set; }

            /// <summary>Milímetros de la muestra que cubre una imagen en Y.</summary>
            public double MmPerImageY { get; set; }

            /// <summary>Distancia entre centros de imágenes consecutivas en mm (eje X).</summary>
            public double StepMmX { get; set; }

            /// <summary>Distancia entre centros de imágenes consecutivas en mm (eje Y).</summary>
            public double StepMmY { get; set; }

            /// <summary>Steps del motor a mover entre imágenes en X (Channel1).</summary>
            public int MotorStepX { get; set; }

            /// <summary>Steps del motor a mover entre imágenes en Y (Channel2).</summary>
            public int MotorStepY { get; set; }

            /// <summary>Calibración seleccionada para el eje X.</summary>
            public PicsCalibration CalibrationX { get; set; }

            /// <summary>Calibración seleccionada para el eje Y.</summary>
            public PicsCalibration CalibrationY { get; set; }
        }

        /// <summary>
        /// Calcula el grid de mosaico a partir del área deseada y las calibraciones disponibles.
        /// </summary>
        /// <param name="areaX_mm">Tamaño del área a rastrear en X (milímetros).</param>
        /// <param name="areaY_mm">Tamaño del área a rastrear en Y (milímetros).</param>
        /// <param name="allCalibrations">Calibraciones del GroupCalibration.</param>
        /// <param name="frameWidth">Ancho del frame de la cámara en pixeles.</param>
        /// <param name="frameHeight">Alto del frame de la cámara en pixeles.</param>
        /// <returns>Parámetros calculados del grid.</returns>
        /// <exception cref="InvalidOperationException">Si no hay calibración válida.</exception>
        public static GridResult Calculate(
            decimal areaX_mm,
            decimal areaY_mm,
            List<PicsCalibration> allCalibrations,
            int frameWidth,
            int frameHeight)
        {
            if (allCalibrations == null || allCalibrations.Count == 0)
                throw new InvalidOperationException("No hay calibraciones disponibles para el grupo.");

            // 1. Seleccionar la mejor calibración para cada eje:
            //    - Filtrar por AxisMovementName ('x' o 'y')
            //    - Ordenar por Confidence descendente (más cercano a 1 = mejor)
            var calX = SelectBestCalibration(allCalibrations, "x");
            var calY = SelectBestCalibration(allCalibrations, "y");

            if (calX == null)
                throw new InvalidOperationException("No se encontró una calibración válida para el eje X (AxisMovementName='x').");
            if (calY == null)
                throw new InvalidOperationException("No se encontró una calibración válida para el eje Y (AxisMovementName='y').");

            if (!double.TryParse(calX.Dx, out var dxPx))
                throw new InvalidOperationException($"La calibración X tiene un valor Dx inválido: '{calX.Dx}'.");
            if (!double.TryParse(calY.Dy, out var dyPx))
                throw new InvalidOperationException($"La calibración Y tiene un valor Dy inválido: '{calY.Dy}'.");
            if (!double.TryParse(calX.MovementValue, out var stepsX) || stepsX <= 0)
                throw new InvalidOperationException($"La calibración X tiene un valor MovementValue inválido: '{calX.MovementValue}'.");
            if (!double.TryParse(calY.MovementValue, out var stepsY) || stepsY <= 0)
                throw new InvalidOperationException($"La calibración Y tiene un valor MovementValue inválido: '{calY.MovementValue}'.");

            // 2. Calcular distancia física total movida durante la calibración
            //    MovementValue = número de steps, cada step = NmPerStep nm
            double totalNmX = stepsX * NmPerStep;
            double totalNmY = stepsY * NmPerStep;
            double totalMmX = totalNmX / 1_000_000.0; // nm → mm
            double totalMmY = totalNmY / 1_000_000.0;

            if (totalMmX <= 0)
                throw new InvalidOperationException("La distancia total calibrada en X es cero o negativa.");
            if (totalMmY <= 0)
                throw new InvalidOperationException("La distancia total calibrada en Y es cero o negativa.");

            // 3. Pixeles por mm: desplazamiento en pixeles / distancia física en mm
            double pixelsPerMmX = Math.Abs(dxPx) / totalMmX;
            double pixelsPerMmY = Math.Abs(dyPx) / totalMmY;

            if (pixelsPerMmX <= 0)
                throw new InvalidOperationException("No se pudo calcular pixeles por mm en X (Dx=0 o distancia=0).");
            if (pixelsPerMmY <= 0)
                throw new InvalidOperationException("No se pudo calcular pixeles por mm en Y (Dy=0 o distancia=0).");

            // 4. Milímetros de la muestra que cubre una imagen completa
            double mmPerImageX = frameWidth / pixelsPerMmX;
            double mmPerImageY = frameHeight / pixelsPerMmY;

            // 5. Distancia entre centros de imágenes consecutivas
            //    Con 70% overlap, se avanza el 30% del tamaño de la imagen
            double advanceRatio = 1.0 - OverlapRatio; // 0.30
            double stepMmX = mmPerImageX * advanceRatio;
            double stepMmY = mmPerImageY * advanceRatio;

            // 6. Número de imágenes necesarias para cubrir el área + 1 (garantiza cobertura del borde final)
            double areaX = (double)areaX_mm;
            double areaY = (double)areaY_mm;
            int imagesX = (int)Math.Ceiling(areaX / stepMmX) + 1;
            int imagesY = (int)Math.Ceiling(areaY / stepMmY) + 1;

            // Asegurar mínimo 1 imagen
            if (imagesX < 1) imagesX = 1;
            if (imagesY < 1) imagesY = 1;

            // 7. Convertir distancia entre centros (mm) a steps del motor
            //    distancia_mm * 1_000_000 (nm/mm) / NmPerStep (nm/step) = steps
            int motorStepX = (int)Math.Round(stepMmX * 1_000_000.0 / NmPerStep);
            int motorStepY = (int)Math.Round(stepMmY * 1_000_000.0 / NmPerStep);

            if (motorStepX < 1) motorStepX = 1;
            if (motorStepY < 1) motorStepY = 1;

            return new GridResult
            {
                ImagesX = imagesX,
                ImagesY = imagesY,
                PixelsPerMmX = pixelsPerMmX,
                PixelsPerMmY = pixelsPerMmY,
                MmPerImageX = mmPerImageX,
                MmPerImageY = mmPerImageY,
                StepMmX = stepMmX,
                StepMmY = stepMmY,
                MotorStepX = motorStepX,
                MotorStepY = motorStepY,
                CalibrationX = calX,
                CalibrationY = calY
            };
        }

        /// <summary>
        /// Selecciona la mejor calibración para un eje dado:
        /// filtra por AxisMovementName (ignorando mayúsculas) y
        /// ordena por confianza descendente (más cercano a 1 = mejor).
        /// </summary>
        private static PicsCalibration SelectBestCalibration(
            List<PicsCalibration> calibrations, string axis)
        {
            var target = axis.ToLowerInvariant();
            return calibrations
                .Where(c => !string.IsNullOrWhiteSpace(c.AxisMovementName)
                            && c.AxisMovementName.ToLowerInvariant() == target)
                .Where(c => !string.IsNullOrWhiteSpace(c.Confidence)
                            && double.TryParse(c.Confidence, out var conf) && conf > 0)
                .OrderByDescending(c =>
                {
                    double.TryParse(c.Confidence, out var conf);
                    // confianza más cercana a 1 → mejor
                    return 1.0 - Math.Abs(1.0 - conf);
                })
                .FirstOrDefault();
        }
    }
}