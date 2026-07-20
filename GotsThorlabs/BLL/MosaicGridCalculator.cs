using GotsThorlabs.Database.EntityRepo.Entities;
using Microsoft.Win32;
using System.Drawing;
using System.Globalization;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Intrinsics.X86;
using System.Windows.Controls;
using static Emgu.CV.Structure.MCvMatND;
using static Emgu.Util.Platform;
using static uEye.DeviceFeatureAOIMerge;

namespace GotsThorlabs.BLL
{
    //comentarios de Clude lo que hizo 
    //Listo — compila sin errores (`Compilación correcta`, solo warnings preexistentes de paquetes). Esto es lo que cambié:

    //## [MosaicGridCalculator.cs](BLL/MosaicGridCalculator.cs) — reescrito el modelo de cálculo

    //- ** Regresión lineal por el origen en vez de "elegir un registro"**: `FitAxis` ahora usa *todos * los registros del eje con steps > 0 y ajusta `desplazamiento = pendiente × steps` para dx y dy por separado.El peso natural es steps², así el registro de 1000 steps domina (mejor señal/ruido) y los de 1 y 10 steps casi no influyen.Desapareció la selección por "confianza cercana a 1", que era la que elegía registros de ruido.
    //- **Detección del eje de imagen dominante**: ya no asume que motor X → Dx.Compara las pendientes de dx y dy y usa la mayor (en tu calibración vieja, motor X movía la imagen en **dy** — cámara rotada 90°). El `GridResult` ahora expone `ImageAxisX/Y` ("dx"/"dy"), `ImageDirectionX/Y` (signo) y `PxPerStepX/Y`.
    //- **`MotorStepX/Y` se calcula directo en píxeles**: `steps = 0.3 × dimensión_frame / px_por_step`, sin pasar por nanómetros — tal como intuías, el 30 nm se cancelaba y ahora ni aparece en ese camino.Además usa la dimensión de frame correcta según el eje de imagen dominante (si el motor X mueve la imagen verticalmente, el avance consume alto de frame, no ancho).
    //- **`NmPerStep` queda solo para lo informativo**: mm por imagen, px/mm y la conversión del área solicitada(mm) a número de imágenes, con comentario explícito de que es nominal y aproximado.
    //- **Guardia anti-ruido**: si el registro más largo de un eje midió menos de 2 px de desplazamiento total, lanza excepción con mensaje claro ("es ruido, no movimiento, revise el motor y recalibre") en lugar de producir un grid absurdo.Con tu calibración vieja del eje Y roto, esto habría avisado del problema físico de inmediato.
    //- **Parseo tolerante a la cultura**: `TryParseDouble` intenta primero `InvariantCulture` (punto decimal, como está en la DB) y luego la cultura actual, para que un cambio de configuración regional de Windows no rompa la lectura.

    //## [PicsCalibrationService.cs](Services/PicsCalibrationService.cs)

    //- Los valores `Dx`, `Dy` y `Confidence` ahora se guardan con `CultureInfo.InvariantCulture`, consistente con el parseo.

    //Compatibilidad: `TakeTour` solo consume `ImagesX/Y`, `MotorStepX/Y` y `CalibrationX` (ahora es el registro de más steps del eje), todo preservado — no necesitó cambios.

    //Una observación pendiente que te dejo decidir: el armado del mosaico en[TakeTour.cs:198 - 212] (BLL/TakeTour.cs:198) (`VConcat` por columna + `HConcat`) asume que motor Y = eje vertical de la imagen.Si la cámara sigue rotada 90° respecto a los motores (como mostraba la calibración vieja), el mosaico crudo saldrá transpuesto.Los campos nuevos `ImageAxisX/Y` del `GridResult` ya dan la información para transponer el armado si hace falta — dime si la calibración nueva sigue mostrando el intercambio de ejes y lo implemento.

    /// <summary>
    /// Calcula los parámetros del grid de mosaico basándose en:
    /// - El tamaño del área a rastrear (mm)
    /// - Los registros de calibración (PicsCalibration) del grupo
    /// - La resolución de la cámara
    /// La calibración se modela como una regresión lineal por el origen
    /// (steps → desplazamiento en pixeles) usando TODOS los registros del eje,
    /// no un solo registro. El dato clave es pixeles por step; el cálculo de
    /// MotorStepX/Y no depende del tamaño físico del step del motor.
    /// También detecta en qué eje de la imagen (dx o dy) se refleja el
    /// movimiento de cada canal del motor, porque la cámara puede estar
    /// rotada respecto a los ejes mecánicos.
    /// </summary>
    public static class MosaicGridCalculator
    {
        /// <summary>
        /// Tamaño físico NOMINAL de cada step del motor en nanómetros.
        /// Solo se usa para los valores informativos en milímetros
        /// (StepMm, MmPerImage, PixelsPerMm) y para convertir el área
        /// solicitada (mm) en número de imágenes. El cálculo de MotorStepX/Y
        /// se hace directamente en pixeles por step y NO depende de este valor.
        /// El KIM101 permite configurar el jog entre 10 y 30 nm por step.
        /// </summary>
        public const double NmPerStep = 30.0; // 30 nm = 0.00003 mm por step

        /// <summary>
        /// Fracción de la imagen que se superpone con la siguiente (70%).
        /// El avance entre imágenes es el 30% del tamaño de la imagen.
        /// </summary>
        public const double OverlapRatio = 0.70;

        /// <summary>
        /// Desplazamiento total mínimo (en pixeles) que debe haber medido el
        /// registro de calibración con más steps para considerar que la
        /// calibración capturó movimiento real y no ruido subpíxel.
        /// </summary>
        public const double MinTotalDisplacementPx = 2.0;

        /// <summary>
        /// Resultado del cálculo del grid de mosaico.
        /// </summary>
        public class GridResult
        {
            /// <summary>Número de imágenes a tomar en el eje X (columnas).</summary>
            public int ImagesX { get; set; }

            /// <summary>Número de imágenes a tomar en el eje Y (filas).</summary>
            public int ImagesY { get; set; }

            /// <summary>Pixeles que se desplaza la imagen por cada step del motor en X (Channel1).</summary>
            public double PxPerStepX { get; set; }

            /// <summary>Pixeles que se desplaza la imagen por cada step del motor en Y (Channel2).</summary>
            public double PxPerStepY { get; set; }

            /// <summary>
            /// Eje de la imagen ("dx" o "dy") donde se refleja el movimiento del motor X.
            /// Si la cámara está rotada 90°, mover el motor en X desplaza la imagen en dy.
            /// </summary>
            public string ImageAxisX { get; set; }

            /// <summary>Eje de la imagen ("dx" o "dy") donde se refleja el movimiento del motor Y.</summary>
            public string ImageAxisY { get; set; }

            /// <summary>Signo (+1/-1) del desplazamiento de la imagen al mover el motor X en positivo.</summary>
            public int ImageDirectionX { get; set; }

            /// <summary>Signo (+1/-1) del desplazamiento de la imagen al mover el motor Y en positivo.</summary>
            public int ImageDirectionY { get; set; }

            /// <summary>Pixeles de la cámara que equivalen a 1 mm en el eje X (usa NmPerStep nominal).</summary>
            public double PixelsPerMmX { get; set; }

            /// <summary>Pixeles de la cámara que equivalen a 1 mm en el eje Y (usa NmPerStep nominal).</summary>
            public double PixelsPerMmY { get; set; }

            /// <summary>Milímetros de la muestra que cubre una imagen en X (estimado con NmPerStep nominal).</summary>
            public double MmPerImageX { get; set; }

            /// <summary>Milímetros de la muestra que cubre una imagen en Y (estimado con NmPerStep nominal).</summary>
            public double MmPerImageY { get; set; }

            /// <summary>Distancia entre centros de imágenes consecutivas en mm (eje X, estimado).</summary>
            public double StepMmX { get; set; }

            /// <summary>Distancia entre centros de imágenes consecutivas en mm (eje Y, estimado).</summary>
            public double StepMmY { get; set; }

            /// <summary>Steps del motor a mover entre imágenes en X (Channel1).</summary>
            public int MotorStepX { get; set; }

            /// <summary>Steps del motor a mover entre imágenes en Y (Channel2).</summary>
            public int MotorStepY { get; set; }

            /// <summary>Registro de calibración representativo del eje X (el de más steps usado en la regresión).</summary>
            public PicsCalibration CalibrationX { get; set; }

            /// <summary>Registro de calibración representativo del eje Y (el de más steps usado en la regresión).</summary>
            public PicsCalibration CalibrationY { get; set; }
        }

        /// <summary>
        /// Resultado del ajuste lineal por el origen de un eje del motor.
        /// </summary>
        private class AxisFit
        {
            /// <summary>Pendiente dx/steps (pixeles horizontales por step).</summary>
            public double SlopeDx;
            /// <summary>Pendiente dy/steps (pixeles verticales por step).</summary>
            public double SlopeDy;
            /// <summary>Magnitud del desplazamiento por step: sqrt(SlopeDx² + SlopeDy²).</summary>
            public double PxPerStep;
            /// <summary>Eje de la imagen con mayor pendiente: "dx" o "dy".</summary>
            public string DominantAxis;
            /// <summary>Signo de la pendiente dominante (+1/-1).</summary>
            public int Direction;
            /// <summary>Steps del registro más largo usado en la regresión.</summary>
            public double MaxSteps;
            /// <summary>Registro de calibración con más steps (mejor relación señal/ruido).</summary>
            public PicsCalibration Representative;
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

            var fitX = FitAxis(allCalibrations, "x");
            var fitY = FitAxis(allCalibrations, "y");

            if (fitX == null)
                throw new InvalidOperationException("No se encontró ninguna calibración válida para el eje X (AxisMovementName='x' con steps > 0).");
            if (fitY == null)
                throw new InvalidOperationException("No se encontró ninguna calibración válida para el eje Y (AxisMovementName='y' con steps > 0).");

            // Validar que la calibración midió movimiento real y no ruido subpíxel:
            // el registro más largo debe haber producido un desplazamiento apreciable.
            double maxDispX = fitX.PxPerStep * fitX.MaxSteps;
            double maxDispY = fitY.PxPerStep * fitY.MaxSteps;
            if (maxDispX < MinTotalDisplacementPx)
                throw new InvalidOperationException(
                    $"La calibración del eje X solo midió {maxDispX:F3} px en {fitX.MaxSteps} steps: es ruido, no movimiento. Revise el motor/acople y recalibre.");
            if (maxDispY < MinTotalDisplacementPx)
                throw new InvalidOperationException(
                    $"La calibración del eje Y solo midió {maxDispY:F3} px en {fitY.MaxSteps} steps: es ruido, no movimiento. Revise el motor/acople y recalibre.");

            // Dimensión del frame sobre la que avanza cada eje del motor:
            // si el motor X mueve la imagen en dy (cámara rotada), el avance
            // entre imágenes consecutivas en X consume alto de frame, no ancho.
            int framePxX = fitX.DominantAxis == "dx" ? frameWidth : frameHeight;
            int framePxY = fitY.DominantAxis == "dx" ? frameWidth : frameHeight;

            // Steps del motor entre imágenes: avance deseado en pixeles / pixeles por step.
            // Cálculo directo en pixeles, sin pasar por nanómetros.
            double advanceRatio = 1.0 - OverlapRatio; // 0.30
            double advancePxX = framePxX * advanceRatio;
            double advancePxY = framePxY * advanceRatio;

            long motorStepXLong = (long)Math.Round(advancePxX / fitX.PxPerStep);
            long motorStepYLong = (long)Math.Round(advancePxY / fitY.PxPerStep);
            int motorStepX = (int)Math.Clamp(motorStepXLong, 1, int.MaxValue);
            int motorStepY = (int)Math.Clamp(motorStepYLong, 1, int.MaxValue);

            // Valores informativos en mm, usando el tamaño nominal del step.
            // OJO: el step real del actuador piezo-inercial varía con carga y
            // dirección, así que estos valores son estimados.
            double mmPerStepNominal = NmPerStep / 1_000_000.0;
            double pixelsPerMmX = fitX.PxPerStep / mmPerStepNominal;
            double pixelsPerMmY = fitY.PxPerStep / mmPerStepNominal;
            double mmPerImageX = framePxX / pixelsPerMmX;
            double mmPerImageY = framePxY / pixelsPerMmY;
            double stepMmX = motorStepX * mmPerStepNominal;
            double stepMmY = motorStepY * mmPerStepNominal;

            // Número de imágenes necesarias para cubrir el área + 1 (garantiza cobertura del borde final)
            double areaX = (double)areaX_mm;
            double areaY = (double)areaY_mm;
            int imagesX = (int)Math.Ceiling(areaX / stepMmX) + 1;
            int imagesY = (int)Math.Ceiling(areaY / stepMmY) + 1;

            if (imagesX < 1) imagesX = 1;
            if (imagesY < 1) imagesY = 1;

            return new GridResult
            {
                ImagesX = imagesX,
                ImagesY = imagesY,
                PxPerStepX = fitX.PxPerStep,
                PxPerStepY = fitY.PxPerStep,
                ImageAxisX = fitX.DominantAxis,
                ImageAxisY = fitY.DominantAxis,
                ImageDirectionX = fitX.Direction,
                ImageDirectionY = fitY.Direction,
                PixelsPerMmX = pixelsPerMmX,
                PixelsPerMmY = pixelsPerMmY,
                MmPerImageX = mmPerImageX,
                MmPerImageY = mmPerImageY,
                StepMmX = stepMmX,
                StepMmY = stepMmY,
                MotorStepX = motorStepX,
                MotorStepY = motorStepY,
                CalibrationX = fitX.Representative,
                CalibrationY = fitY.Representative
            };
        }

        /// <summary>
        /// Ajusta una recta por el origen (desplazamiento = pendiente × steps)
        /// para cada componente de la imagen (dx, dy) usando todos los registros
        /// del eje con steps > 0. La regresión pondera naturalmente los registros
        /// de más steps (mejor señal/ruido) porque el peso es steps².
        /// Devuelve null si no hay ningún registro utilizable.
        /// </summary>
        private static AxisFit FitAxis(List<PicsCalibration> calibrations, string axis)
        {
            var target = axis.ToLowerInvariant();
            var points = new List<(double Steps, double Dx, double Dy, PicsCalibration Record)>();

            foreach (var c in calibrations)
            {
                if (string.IsNullOrWhiteSpace(c.AxisMovementName)
                    || c.AxisMovementName.ToLowerInvariant() != target)
                    continue;
                if (!TryParseDouble(c.MovementValue, out var steps) || steps <= 0)
                    continue;
                if (!TryParseDouble(c.Dx, out var dx) || !TryParseDouble(c.Dy, out var dy))
                    continue;

                points.Add((steps, dx, dy, c));
            }

            if (points.Count == 0)
                return null;

            double sumS2 = points.Sum(p => p.Steps * p.Steps);
            if (sumS2 <= 0)
                return null;

            double slopeDx = points.Sum(p => p.Steps * p.Dx) / sumS2;
            double slopeDy = points.Sum(p => p.Steps * p.Dy) / sumS2;
            double pxPerStep = Math.Sqrt(slopeDx * slopeDx + slopeDy * slopeDy);
            if (pxPerStep <= 0)
                return null;

            var longest = points.OrderByDescending(p => p.Steps).First();
            string dominant = Math.Abs(slopeDx) >= Math.Abs(slopeDy) ? "dx" : "dy";
            double dominantSlope = dominant == "dx" ? slopeDx : slopeDy;

            return new AxisFit
            {
                SlopeDx = slopeDx,
                SlopeDy = slopeDy,
                PxPerStep = pxPerStep,
                DominantAxis = dominant,
                Direction = dominantSlope >= 0 ? 1 : -1,
                MaxSteps = longest.Steps,
                Representative = longest.Record
            };
        }

        /// <summary>
        /// Parsea un double aceptando tanto punto (formato invariante, como se
        /// guarda en la DB) como el separador decimal de la cultura actual.
        /// Evita que un cambio de configuración regional de Windows rompa la
        /// lectura de la calibración.
        /// </summary>
        private static bool TryParseDouble(string s, out double value)
        {
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return true;
            return double.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }
    }
}
