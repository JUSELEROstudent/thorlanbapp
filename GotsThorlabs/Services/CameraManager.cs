//using System;
//using System.Drawing;
//using System.Drawing.Imaging;
//using System.IO;
//using System.Threading;
//using ueye;
//using uEye.Defines;
//using uEye.Types;

//namespace GotsThorlabs.Services
//{
//    public class CameraManager
//    {
//        /// <summary>
//        /// Captura una imagen desde una cámara uEye y la guarda en la raíz de la aplicación
//        /// </summary>
//        /// <param name="cameraId">ID de la cámara (default: 0 para la primera cámara)</param>
//        /// <param name="timeoutMs">Tiempo de espera en ms para la captura</param>
//        /// <param name="fileName">Nombre del archivo de salida (default: "captured_image.png")</param>
//        /// <returns>Ruta del archivo guardado o mensaje de error</returns>
//        public static string CaptureAndSaveImage(int cameraId = 0, int timeoutMs = 5000, string fileName = "captured_image.png")
//        {
//            Camera camera = new open_camera();
//            Int32 status;
//            string resultMessage;

//            try
//            {
//                // Inicializar la cámara con el ID especificado
//                status = camera.Init(cameraId);
//                if (status != Status.SUCCESS)
//                {
//                    return $"Error al inicializar la cámara (ID: {cameraId}). Código: {status}. Verifique la conexión.";
//                }

//                // Obtener información de la cámara
//                CameraInformation camInfo;
//                camera.Information.GetCameraInfo(out camInfo);
//                Console.WriteLine($"Cámara conectada: {camInfo.SensorName}, S/N: {camInfo.SerialNumber}");

//                // Configurar parámetros de la cámara
//                camera.PixelFormat.Set(ColorMode.RGB8);

//                // Asignar memoria para la imagen
//                Int32 memoryId = 0;
//                status = camera.Memory.Allocate(ref memoryId);
//                if (status != Status.SUCCESS)
//                {
//                    camera.Exit();
//                    return $"Error al asignar memoria para la imagen. Código: {status}";
//                }

//                // Capturar una imagen (modo Snap = una sola imagen)
//                status = camera.Acquisition.Freeze();
//                if (status != Status.SUCCESS)
//                {
//                    camera.Exit();
//                    return $"Error al capturar la imagen. Código: {status}";
//                }

//                // Esperar a que la imagen esté disponible
//                bool imageReady = false;
//                DateTime startTime = DateTime.Now;
//                while (!imageReady)
//                {
//                    if ((DateTime.Now - startTime).TotalMilliseconds > timeoutMs)
//                    {
//                        camera.Exit();
//                        return "Tiempo de espera agotado al capturar la imagen";
//                    }

//                    camera.Information.GetImageInfo(memoryId, out var imageInfo);
//                    imageReady = imageInfo.ImageStatus == uEye.Defines.TImageStatus.IS_IMG_READY;
//                    if (!imageReady)
//                    {
//                        Thread.Sleep(10);
//                    }
//                }

//                // Obtener la imagen como Bitmap
//                Bitmap bmp = null;
//                status = camera.Memory.ToBitmap(memoryId, out bmp);
//                if (status != Status.SUCCESS || bmp == null)
//                {
//                    camera.Exit();
//                    return $"Error al convertir la imagen a Bitmap. Código: {status}";
//                }

//                // Determinar la ruta de guardado (raíz de la aplicación)
//                string appRoot = AppDomain.CurrentDomain.BaseDirectory;
//                string fullPath = Path.Combine(appRoot, fileName);

//                // Guardar la imagen
//                bmp.Save(fullPath, ImageFormat.Png);

//                resultMessage = $"Imagen guardada exitosamente en: {fullPath}";

//                // Liberar recursos
//                bmp.Dispose();
//            }
//            catch (Exception ex)
//            {
//                resultMessage = $"Error inesperado: {ex.Message}";
//            }
//            finally
//            {
//                // Asegurarse de liberar la cámara en todos los casos
//                if (camera != null && camera.IsOpened)
//                {
//                    camera.Exit();
//                }
//            }

//            return resultMessage;
//        }

//        // Ejemplo de uso:
//        public static void Main(string[] args)
//        {
//            try
//            {
//                Console.WriteLine("Intentando capturar imagen con cámara uEye...");
//                string result = CaptureAndSaveImage();
//                Console.WriteLine(result);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error en la aplicación principal: {ex.Message}");
//            }

//            Console.WriteLine("Presione cualquier tecla para salir...");
//            Console.ReadKey();
//        }
//    }
//}