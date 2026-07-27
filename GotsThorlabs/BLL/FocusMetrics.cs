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
        /// Varianza del Laplaciano: métrica estándar de nitidez en visión por
        /// computadora. El Laplaciano responde a los bordes de la imagen; cuando
        /// la muestra está desenfocada los bordes se difuminan, la respuesta se
        /// aplana y su varianza cae. A mayor valor, más nítida la imagen.
        ///
        /// La escala del valor NO es absoluta: depende de la muestra, del objetivo
        /// y de la resolución de la cámara. Por eso el umbral de aceptación se
        /// guarda por cámara (campo focusThreshold en settingsJson) en lugar de
        /// fijarse como constante global, y la comparación útil es siempre entre
        /// medidas del mismo montaje óptico.
        /// </summary>
        /// <param name="frame">Imagen a evaluar (BGR o escala de grises).</param>
        /// <returns>Varianza del Laplaciano; 0 si la imagen es nula o está vacía.</returns>
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

            // La desviación estándar se calcula sobre el LAPLACIANO, no sobre la
            // imagen original: ese es justamente el punto de la métrica.
            Cv2.MeanStdDev(laplacian, out var mean, out var stddev);

            return stddev.Val0 * stddev.Val0;
        }
    }
}
