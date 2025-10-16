using GotsThorlabs.Interfaces;
using static OpenCvSharp.FileStorage;
using OpenCvSharp;

namespace GotsThorlabs.BLL
{
    public class ProcessTourData : IMakeAnStitching
    {
        public string ProcessTourDatawWhitStitchingPanorama(string PathName)
        {
            var carpetaPath = Path.Combine(Environment.CurrentDirectory, "StaticFiles", "datasetstitched");
            string[] archivos = Directory.GetFiles(carpetaPath, "*.jpg");
            Mat[] arraisMat = new Mat[archivos.Length];
            var output = new Mat();
            var outputarray1 = new Mat();
            int indexinter = 0;

            foreach (var archivo in archivos)
            {
                var img = new Mat(archivo.ToString());
                arraisMat[indexinter] = img;
                indexinter++;
            }

            var mode = OpenCvSharp.Stitcher.Mode.Panorama;
            var stitched = Stitcher.Create(mode);
            //var nuevo1 = new BestOf2NearestMatcher();

            var solucion = stitched.Stitch(arraisMat, output);
            var estado = solucion == Stitcher.Status.OK ? true : false;
            var direccionsave = Path.Combine(Environment.CurrentDirectory, "StaticFiles", "openNative.jpg");
            output.SaveImage(direccionsave);
            return direccionsave;
            throw new NotImplementedException();
        }

        public string ProcessTourDatawWhitStitchingScans(string PathName)
        {
            var carpetaPath = Path.Combine(Environment.CurrentDirectory, "StaticFiles", PathName);
            string[] archivos = Directory.GetFiles(carpetaPath, "unitofpics*.jpg");
            Mat[] arraisMat = new Mat[archivos.Length];
            var output = new Mat();
            var outputarray1 = new Mat();
            int indexinter = 0;

            foreach (var archivo in archivos)
            {
                var img = new Mat(archivo.ToString());
                arraisMat[indexinter] = img;
                indexinter++;
            }

            var mode = OpenCvSharp.Stitcher.Mode.Scans;
            var stitched = Stitcher.Create(mode);
            //var nuevo1 = new BestOf2NearestMatcher();

            var solucion = stitched.Stitch(arraisMat, output);
            var estado = solucion == Stitcher.Status.OK ? true : false;
            var direccionsave = Path.Combine(Environment.CurrentDirectory, "StaticFiles", PathName, "openNative.jpg");
            output.SaveImage(direccionsave);
            return direccionsave;
        }
    }

    public static class PhaseCorrelationDetector
    {
        /// <summary>
        /// Detecta el desplazamiento entre dos imágenes usando Phase Correlation
        /// </summary>
        /// <param name="img1">Primera imagen (referencia)</param>
        /// <param name="img2">Segunda imagen (desplazada)</param>
        /// <returns>Punto con el desplazamiento (dx, dy) y confianza</returns>
        public static (Point2d shift, double confidence) DetectShift(Mat img1, Mat img2)
        {
            // Convertir a escala de grises si es necesario
            Mat gray1 = new Mat();
            Mat gray2 = new Mat();

            if (img1.Channels() == 3)
                Cv2.CvtColor(img1, gray1, ColorConversionCodes.BGR2GRAY);
            else
                gray1 = img1.Clone();

            if (img2.Channels() == 3)
                Cv2.CvtColor(img2, gray2, ColorConversionCodes.BGR2GRAY);
            else
                gray2 = img2.Clone();

            // Convertir a float32 para mejor precisión
            Mat float1 = new Mat();
            Mat float2 = new Mat();
            gray1.ConvertTo(float1, MatType.CV_32F);
            gray2.ConvertTo(float2, MatType.CV_32F);

            // Aplicar ventana Hanning para reducir efectos de borde
            Mat hann = new Mat();
            Cv2.CreateHanningWindow(hann, float1.Size(), MatType.CV_32F);

            Cv2.Multiply(float1, hann, float1);
            Cv2.Multiply(float2, hann, float2);

            // Calcular FFT de ambas imágenes
            Mat fft1 = ComputeFFT(float1);
            Mat fft2 = ComputeFFT(float2);

            // Calcular la correlación cruzada en el dominio de frecuencia
            Mat crossCorr = ComputeCrossCorrelation(fft1, fft2);

            // Convertir de vuelta al dominio espacial
            Mat result = new Mat();
            Cv2.Dft(crossCorr, result, DftFlags.Inverse | DftFlags.Scale);

            // Encontrar el pico máximo
            Point maxLoc;
            double maxVal;
            Cv2.MinMaxLoc(result, out double minVal, out maxVal, out Point minLoc, out maxLoc);

            // Convertir coordenadas del pico a desplazamiento
            Point2d shift = ConvertToShift(maxLoc, result.Size());
            double confidence = maxVal / (result.Rows * result.Cols);

            // Limpiar memoria
            gray1?.Dispose();
            gray2?.Dispose();
            float1?.Dispose();
            float2?.Dispose();
            hann?.Dispose();
            fft1?.Dispose();
            fft2?.Dispose();
            crossCorr?.Dispose();
            result?.Dispose();

            return (shift, confidence);
        }

        /// <summary>
        /// Calcula la FFT 2D de una imagen
        /// </summary>
        private static Mat ComputeFFT(Mat image)
        {
            Mat padded = new Mat();

            // Expandir la imagen a un tamaño óptimo para FFT
            int m = Cv2.GetOptimalDFTSize(image.Rows);
            int n = Cv2.GetOptimalDFTSize(image.Cols);

            Cv2.CopyMakeBorder(image, padded, 0, m - image.Rows, 0, n - image.Cols,
                              BorderTypes.Constant, Scalar.All(0));

            // Crear un array de 2 canales (real, imaginario)
            Mat[] planes = { padded, Mat.Zeros(padded.Size(), MatType.CV_32F) };
            Mat complex = new Mat();
            Cv2.Merge(planes, complex);

            // Aplicar DFT
            Cv2.Dft(complex, complex);

            padded?.Dispose();
            planes[1]?.Dispose();

            return complex;
        }

        /// <summary>
        /// Calcula la correlación cruzada normalizada en frecuencia
        /// </summary>
        private static Mat ComputeCrossCorrelation(Mat fft1, Mat fft2)
        {
            // Conjugado de fft2
            Mat fft2Conj = new Mat();
            Cv2.MulSpectrums(fft2, new Mat(), fft2Conj, DftFlags.Rows, true);

            // Multiplicación elemento por elemento
            Mat product = new Mat();
            Cv2.MulSpectrums(fft1, fft2Conj, product, DftFlags.None, false);

            // Normalización para phase correlation
            Mat[] planes = new Mat[2];
            Cv2.Split(product, out planes);

            Mat magnitude = new Mat();
            Cv2.Magnitude(planes[0], planes[1], magnitude);

            // Evitar división por cero
            Cv2.Threshold(magnitude, magnitude, 1e-10, 0, ThresholdTypes.Tozero);

            // Normalizar
            Cv2.Divide(planes[0], magnitude, planes[0]);
            Cv2.Divide(planes[1], magnitude, planes[1]);

            Mat normalized = new Mat();
            Cv2.Merge(planes, normalized);

            // Limpiar memoria
            fft2Conj?.Dispose();
            product?.Dispose();
            planes[0]?.Dispose();
            planes[1]?.Dispose();
            magnitude?.Dispose();

            return normalized;
        }

        /// <summary>
        /// Convierte las coordenadas del pico a desplazamiento real
        /// </summary>
        private static Point2d ConvertToShift(Point maxLoc, Size size)
        {
            double dx = maxLoc.X;
            double dy = maxLoc.Y;

            // Ajustar para coordenadas centradas
            if (dx > size.Width / 2)
                dx -= size.Width;
            if (dy > size.Height / 2)
                dy -= size.Height;

            return new Point2d(dx, dy);
        }

        /// <summary>
        /// Versión simplificada usando la función nativa de OpenCV
        /// </summary>
        public static (Point2d shift, double confidence) DetectShiftSimple(Mat img1, Mat img2)
        {
            // Convertir a escala de grises
            Mat gray1 = new Mat();
            Mat gray2 = new Mat();

            if (img1.Channels() == 3)
                Cv2.CvtColor(img1, gray1, ColorConversionCodes.BGR2GRAY);
            else
                gray1 = img1.Clone();

            if (img2.Channels() == 3)
                Cv2.CvtColor(img2, gray2, ColorConversionCodes.BGR2GRAY);
            else
                gray2 = img2.Clone();

            // Usar la función nativa de OpenCV (si está disponible)
            Mat hann = new Mat();
            //Point2d shift = Cv2.PhaseCorrelate(gray1, gray2, out Mat hann, out double response);
            Point2d shift = Cv2.PhaseCorrelate(gray1, gray2, hann, out double response);

            gray1?.Dispose();
            gray2?.Dispose();
            hann?.Dispose();

            return (shift, response);
        }
    }

    // Clase helper para casos de uso específicos
    public static class PhaseCorrelationUtils
    {
        /// <summary>
        /// Detecta múltiples desplazamientos en una secuencia de imágenes
        /// </summary>
        public static Point2d[] DetectSequenceShifts(Mat[] images)
        {
            if (images.Length < 2)
                throw new ArgumentException("Se necesitan al menos 2 imágenes");

            Point2d[] shifts = new Point2d[images.Length - 1];

            for (int i = 1; i < images.Length; i++)
            {
                var (shift, confidence) = PhaseCorrelationDetector.DetectShift(images[i - 1], images[i]);
                shifts[i - 1] = shift;

                Console.WriteLine($"Frame {i}: Shift=({shift.X:F2}, {shift.Y:F2}), Confidence={confidence:F4}");
            }

            return shifts;
        }

        /// <summary>
        /// Aplica filtrado a desplazamientos para reducir ruido
        /// </summary>
        public static Point2d[] FilterShifts(Point2d[] shifts, double threshold = 0.1)
        {
            Point2d[] filtered = new Point2d[shifts.Length];

            for (int i = 0; i < shifts.Length; i++)
            {
                // Filtro simple: si el desplazamiento es muy pequeño, lo consideramos ruido
                if (Math.Abs(shifts[i].X) < threshold && Math.Abs(shifts[i].Y) < threshold)
                    filtered[i] = new Point2d(0, 0);
                else
                    filtered[i] = shifts[i];
            }

            return filtered;
        }

    }
}
