using OpenCvSharp;

namespace GotsThorlabs.BLL
{
    /// <summary>
    /// Métricas de nitidez (enfoque) sobre imágenes capturadas.
    ///
    /// Se centraliza aquí para que el valor de enfoque sea el mismo en los tres
    /// lugares donde se usa: el indicador en vivo del streaming, la validación
    /// previa a lanzar una calibración, y el campo gausianVal que se guarda por
    /// cada imagen de un recorrido.
    /// </summary>
    public static class FocusMetrics
    {
        /// <summary>
        /// Identifica la fórmula con la que se calculó un valor de nitidez. Se guarda
        /// junto al umbral de cada cámara (focusMetricVersion en settingsJson): un
        /// umbral medido con otra fórmula está en otra escala y compararlo daría un
        /// veredicto falso, así que se marca como no calibrado en lugar de usarse.
        /// Al cambiar el cálculo hay que cambiar esta constante.
        /// </summary>
        public const string MetricVersion = "vol-norm-2";

        /// <summary>Fracción central del cuadro que se mide (60% de ancho y alto).</summary>
        private const double RoiFraction = 0.6;

        /// <summary>Alto al que se reescala la región antes de medir.</summary>
        private const int WorkingHeight = 512;

        /// <summary>Factor de escala para que el resultado quede en un rango legible.</summary>
        private const double ReadableScale = 10000d;

        /// <summary>
        /// Medida de nitidez normalizada. Es la que debe usarse para comparar contra
        /// un umbral; <see cref="VarianceOfLaplacian"/> cruda no sirve para eso.
        ///
        /// Sobre la varianza del Laplaciano se aplican tres normalizaciones, cada una
        /// para quitar una dependencia que hacía que el número no fuera comparable:
        ///
        /// 1. Región central fija: se mide solo el 60% central del cuadro. Los bordes
        ///    del campo suelen estar peor enfocados (curvatura de campo) y arrastraban
        ///    el valor hacia abajo según cómo estuviera encuadrada la muestra.
        /// 2. Reescalado a alto fijo: el Laplaciano responde al detalle por píxel, así
        ///    que la misma muestra a distinta resolución daba valores distintos. Medir
        ///    siempre sobre la misma escala espacial elimina esa dependencia.
        /// 3. División por la luminancia media al cuadrado: al subir la exposición, la
        ///    respuesta del Laplaciano y la media crecen en la misma proporción, de modo
        ///    que el cociente se mantiene. Sin esto, cambiar la exposición movía el
        ///    número sin que la muestra se hubiera desenfocado.
        ///
        /// Antes del Laplaciano se aplica un suavizado gaussiano 3x3. El ruido de sensor
        /// es detalle de alta frecuencia igual que los bordes, así que eleva la respuesta
        /// del Laplaciano sin que la muestra esté más enfocada; el suavizado reduce esa
        /// contribución en torno a 4x, medido sobre imágenes sintéticas con ruido gaussiano.
        ///
        /// Lo que NO corrige: el ruido por completo. Con ganancia suficientemente alta, una
        /// imagen desenfocada y ruidosa sigue pudiendo puntuar por encima de una enfocada y
        /// limpia — es una limitación de fondo de la varianza del Laplaciano, que no puede
        /// separar detalle por enfoque de detalle por ruido. Lo que cierra ese hueco no es
        /// la fórmula sino el procedimiento: el umbral se aprende con la configuración real
        /// de la cámara y se invalida si esa configuración cambia (ver FocusCriterion), de
        /// modo que el nivel de ruido al medir es el mismo que al calibrar.
        /// </summary>
        /// <param name="frame">Imagen a evaluar (BGR o escala de grises).</param>
        /// <returns>Nitidez normalizada; 0 si la imagen es nula o está vacía.</returns>
        public static double Sharpness(Mat? frame)
        {
            if (frame == null || frame.Empty())
                return 0d;

            using var gray = new Mat();
            if (frame.Channels() == 1)
            {
                frame.CopyTo(gray);
            }
            else
            {
                Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
            }

            var roiWidth = Math.Max(1, (int)Math.Round(gray.Cols * RoiFraction));
            var roiHeight = Math.Max(1, (int)Math.Round(gray.Rows * RoiFraction));
            var roiX = (gray.Cols - roiWidth) / 2;
            var roiY = (gray.Rows - roiHeight) / 2;

            using var roi = new Mat(gray, new Rect(roiX, roiY, roiWidth, roiHeight));

            var targetWidth = Math.Max(1, (int)Math.Round(roi.Cols * (double)WorkingHeight / roi.Rows));
            using var scaled = new Mat();
            Cv2.Resize(roi, scaled, new Size(targetWidth, WorkingHeight), 0, 0, InterpolationFlags.Area);

            var mean = Cv2.Mean(scaled).Val0;

            // Suavizado previo: atenúa el ruido de sensor, que de otro modo el Laplaciano
            // contaría como si fuera detalle de la muestra.
            using var denoised = new Mat();
            Cv2.GaussianBlur(scaled, denoised, new Size(3, 3), 0);

            using var laplacian = new Mat();
            Cv2.Laplacian(denoised, laplacian, MatType.CV_64F);

            // La desviación estándar se calcula sobre el LAPLACIANO, no sobre la
            // imagen original: ese es justamente el punto de la métrica.
            Cv2.MeanStdDev(laplacian, out _, out var stddev);
            var variance = stddev.Val0 * stddev.Val0;

            // El mínimo de 1 evita dividir por cero con un cuadro completamente negro;
            // ese cuadro no tiene bordes, así que la varianza ya es ~0 de todos modos.
            return variance / Math.Max(mean * mean, 1d) * ReadableScale;
        }

        /// <summary>
        /// Varianza del Laplaciano cruda, sin normalizar.
        ///
        /// Se conserva como primitiva y para diagnóstico. NO usarla para decidir si una
        /// imagen está suficientemente enfocada: su valor depende de la muestra, del
        /// objetivo, de la resolución y de la exposición, de modo que un umbral fijo
        /// sobre ella no significa lo mismo de una captura a otra. Para eso está
        /// <see cref="Sharpness"/>.
        /// </summary>
        public static double VarianceOfLaplacian(Mat? frame)
        {
            if (frame == null || frame.Empty())
                return 0d;

            using var gray = new Mat();
            if (frame.Channels() == 1)
            {
                frame.CopyTo(gray);
            }
            else
            {
                Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
            }

            using var laplacian = new Mat();
            Cv2.Laplacian(gray, laplacian, MatType.CV_64F);

            Cv2.MeanStdDev(laplacian, out _, out var stddev);

            return stddev.Val0 * stddev.Val0;
        }
    }
}
