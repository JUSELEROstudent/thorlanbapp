using GotsThorlabs.Models.Configuration;
using GotsThorlabs.BLL.Configuration;
using OpenCvSharp;
using OpenCvSharp.Features2D;

namespace GotsThorlabs.BLL.Configuration
{
    public interface ConfigurationService : IConfigurationService<ConfigurationDTO>
    {
        public ConfigurationDTO CreateAkindOfCalibration(string configName, decimal factorMicroscopio)
        {
            throw new Exception("errior de creacion ");
        }

        static Mat CreateExampleImage()
        {
            Mat image = new Mat(400, 600, MatType.CV_8UC3, Scalar.All(255));

            // Dibujar algunas formas con características detectables
            Cv2.Rectangle(image, new Point(50, 50), new Point(150, 150), Scalar.Red, 2);
            Cv2.Circle(image, new Point(300, 100), 50, Scalar.Blue, 2);
            Cv2.Ellipse(image, new Point(450, 200), new Size(80, 40), 45, 0, 360, Scalar.Green, 2);

            // Agregar algunos puntos y líneas
            Cv2.Line(image, new Point(100, 250), new Point(500, 250), Scalar.Black, 3);
            Cv2.Circle(image, new Point(200, 300), 3, Scalar.Red, -1);
            Cv2.Circle(image, new Point(400, 320), 3, Scalar.Blue, -1);

            // Agregar texto
            Cv2.PutText(image, "SIFT Test", new Point(250, 350),
                       HersheyFonts.HersheySimplex, 1, Scalar.Black, 2);

            return image;
        }

        /// <summary>
        /// Detecta y visualiza keypoints SIFT en una sola imagen
        /// </summary>
        private void DetectKeypointsInSingleImage()
        {
            Console.WriteLine("=== Detección de Keypoints SIFT ===");

            // Cargar imagen (reemplaza con tu ruta de imagen)
            Mat image = Cv2.ImRead("input_image.jpg", ImreadModes.Color);

            if (image.Empty())
            {
                Console.WriteLine("No se pudo cargar la imagen. Creando imagen de ejemplo...");
                // Crear una imagen de ejemplo con formas geométricas
                image = CreateExampleImage();
            }

            // Convertir a escala de grises
            Mat grayImage = new Mat();
            Cv2.CvtColor(image, grayImage, ColorConversionCodes.BGR2GRAY);

            // Crear detector SIFT
            var sift = SIFT.Create(
                //nfeatures: 0,           // 0 = detectar todos los features posibles
                nOctaveLayers: 3,       // número de capas por octava
                contrastThreshold: 0.04, // umbral de contraste
                edgeThreshold: 10,      // umbral de borde
                sigma: 1.6              // sigma gaussiano
            );

            // Detectar keypoints
            KeyPoint[] keypoints = sift.Detect(grayImage);

            Console.WriteLine($"Keypoints detectados: {keypoints.Length}");

            // Calcular descriptores
            Mat descriptors = new Mat();
            sift.Compute(grayImage, ref keypoints, descriptors);

            Console.WriteLine($"Descriptores calculados: {descriptors.Rows} x {descriptors.Cols}");

            // Dibujar keypoints en la imagen
            Mat imageWithKeypoints = new Mat();
            Cv2.DrawKeypoints(image, keypoints, imageWithKeypoints,
                             Scalar.All(-1), DrawMatchesFlags.DrawRichKeypoints);

            // Mostrar resultados
            Cv2.ImShow("Imagen Original", image);
            Cv2.ImShow("SIFT Keypoints", imageWithKeypoints);

            // Guardar resultado
            Cv2.ImWrite("sift_keypoints_result.jpg", imageWithKeypoints);
            Console.WriteLine("Resultado guardado como 'sift_keypoints_result.jpg'");

            Console.WriteLine("Presiona cualquier tecla para continuar...");
            Cv2.WaitKey();
            Cv2.DestroyAllWindows();

            // Liberar recursos
            image.Dispose();
            grayImage.Dispose();
            descriptors.Dispose();
            imageWithKeypoints.Dispose();
            sift.Dispose();
        }
    }
}
