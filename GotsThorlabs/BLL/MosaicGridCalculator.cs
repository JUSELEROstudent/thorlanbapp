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
        /// Límite máximo de imágenes permitidas por eje. Es una válvula de
        /// seguridad adicional (además de <see cref="MaxMosaicBytes"/>) para
        /// detectar de inmediato un grid absurdo (p.ej. producto de un
        /// PxPerStep mal calculado) antes de intentar reservar memoria.
        /// </summary>
        public const int MaxImagesPerAxis = 500;

        /// <summary>
        /// Tamaño máximo (en bytes) que se permite para el mosaico completo
        /// (ImagesX * frameWidth) x (ImagesY * frameHeight) x 3 canales.
        /// Antes de este guard, un cálculo de grid erróneo (p.ej. porque
        /// MotorStepX/Y quedó forzado al mínimo de 1 step por un PxPerStep
        /// anómalo) terminaba en TakeTour.TakeAPic intentando reservar un
        /// Mat de decenas de petabytes y OpenCvSharp lanzaba
        /// "Failed to allocate ... bytes" sin ninguna pista de la causa real.
        /// Con este guard se detecta apenas se calcula el grid, con un
        /// mensaje que apunta al origen (calibración / área solicitada).
        /// 2 GB es un límite conservador: un mosaico razonable (decenas de
        /// imágenes) queda muy por debajo; un mosaico de cientos de miles de
        /// imágenes lo supera de inmediato.
        /// </summary>
        public const long MaxMosaicBytes = 2_000_000_000L;

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

            /// <summary>Nanómetros por paso empleados en el eje X (medidos o nominales).</summary>
            public double StepSizeNmX { get; set; }

            /// <summary>Nanómetros por paso empleados en el eje Y (medidos o nominales).</summary>
            public double StepSizeNmY { get; set; }

            /// <summary>
            /// False cuando algún eje cayó al nominal por no tener caracterización mecánica.
            /// En ese caso el área recorrida en milímetros es una estimación sin verificar.
            /// </summary>
            public bool StepSizeMeasured { get; set; }

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
        /// <param name="measuredNmPerStepX">
        /// Nanómetros por paso medidos con pie de rey para el eje X, o null si esa
        /// caracterización no existe. Cuando falta se cae al nominal <see cref="NmPerStep"/>,
        /// que NO está verificado: el tamaño real de paso de un actuador inercial depende
        /// de la carga y de los parámetros de accionamiento, así que el área que el usuario
        /// pide en milímetros puede no corresponder con la que se recorre.
        /// </param>
        /// <param name="measuredNmPerStepY">Equivalente para el eje Y.</param>
        public static GridResult Calculate(
            decimal areaX_mm,
            decimal areaY_mm,
            List<PicsCalibration> allCalibrations,
            int frameWidth,
            int frameHeight,
            double? measuredNmPerStepX = null,
            double? measuredNmPerStepY = null)
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

            // Conversión a milímetros. Se usa el tamaño de paso MEDIDO por eje cuando
            // existe una caracterización mecánica del motor; si no, se cae al nominal.
            //
            // Esto no es solo informativo: stepMm determina cuántas imágenes hacen falta
            // para cubrir el área que pidió el usuario. Con el nominal equivocado, el
            // recorrido cubre un área distinta de la solicitada sin que nada lo delate.
            bool stepMeasuredX = measuredNmPerStepX is > 0;
            bool stepMeasuredY = measuredNmPerStepY is > 0;
            double nmPerStepX = stepMeasuredX ? measuredNmPerStepX!.Value : NmPerStep;
            double nmPerStepY = stepMeasuredY ? measuredNmPerStepY!.Value : NmPerStep;

            double mmPerStepX = nmPerStepX / 1_000_000.0;
            double mmPerStepY = nmPerStepY / 1_000_000.0;
            double pixelsPerMmX = fitX.PxPerStep / mmPerStepX;
            double pixelsPerMmY = fitY.PxPerStep / mmPerStepY;
            double mmPerImageX = framePxX / pixelsPerMmX;
            double mmPerImageY = framePxY / pixelsPerMmY;
            double stepMmX = motorStepX * mmPerStepX;
            double stepMmY = motorStepY * mmPerStepY;

            // Número de imágenes necesarias para cubrir el área + 1 (garantiza cobertura del borde final)
            double areaX = (double)areaX_mm;
            double areaY = (double)areaY_mm;
            int imagesX = (int)Math.Ceiling(areaX / stepMmX) + 1;
            int imagesY = (int)Math.Ceiling(areaY / stepMmY) + 1;

            if (imagesX < 1) imagesX = 1;
            if (imagesY < 1) imagesY = 1;

            // Guard 1: número de imágenes por eje fuera de todo rango razonable.
            // Esto ya delata el problema (grid absurdo) antes de gastar tiempo
            // calculando el resto o de intentar reservar memoria para el mosaico.
            if (imagesX > MaxImagesPerAxis || imagesY > MaxImagesPerAxis)
                throw new InvalidOperationException(
                    $"Grid calculado fuera de rango: ImagesX={imagesX}, ImagesY={imagesY} " +
                    $"(máximo permitido por eje: {MaxImagesPerAxis}). " +
                    $"StepMmX={stepMmX:G6} mm, StepMmY={stepMmY:G6} mm, " +
                    $"MotorStepX={motorStepX}, MotorStepY={motorStepY}, " +
                    $"PxPerStepX={fitX.PxPerStep:G6}, PxPerStepY={fitY.PxPerStep:G6}. " +
                    $"Es casi seguro que la calibración del grupo está dando un PxPerStep " +
                    $"anómalo (posiblemente porque MotorStepX/Y quedó forzado al mínimo de 1 " +
                    $"step) — revise las calibraciones del GroupCalibration antes de reintentar.");

            // Guard 2: tamaño total del mosaico (bytes) que se intentará reservar
            // en TakeTour.TakeAPic (rows*frameHeight) x (columns*frameWidth) x 3.
            // Se usa 'long' explícitamente para evitar overflow de int en el cálculo.
            long estimatedMosaicBytes = (long)imagesX * frameWidth * (long)imagesY * frameHeight * 3L;
            if (estimatedMosaicBytes > MaxMosaicBytes)
                throw new InvalidOperationException(
                    $"El mosaico calculado ({imagesX}x{imagesY} imágenes de {frameWidth}x{frameHeight}px) " +
                    $"pesaría ~{estimatedMosaicBytes / 1_000_000_000.0:F2} GB, por encima del límite " +
                    $"de seguridad ({MaxMosaicBytes / 1_000_000_000.0:F1} GB). " +
                    $"Revise el área solicitada (X={areaX_mm}mm, Y={areaY_mm}mm) y la calibración " +
                    $"del grupo antes de reintentar.");

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
                CalibrationY = fitY.Representative,
                StepSizeNmX = nmPerStepX,
                StepSizeNmY = nmPerStepY,
                StepSizeMeasured = stepMeasuredX && stepMeasuredY
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
