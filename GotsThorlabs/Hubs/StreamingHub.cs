using GotsThorlabs.BLL;
using Microsoft.AspNetCore.SignalR;
using OpenCvSharp;
using System.Drawing;
using System.Runtime.CompilerServices;
using Thorlabs.MotionControl.DeviceManagerCLI;
using Thorlabs.MotionControl.KCube.InertialMotorCLI;

namespace GotsThorlabs.Hubs
{
    public class StreamingHub : Hub
    {
        public async IAsyncEnumerable<byte[]> Counter(
         int count,
         int delay,
         [EnumeratorCancellation]
        CancellationToken cancellationToken)
        {
            var idconection = Context.ConnectionAborted;
            Console.WriteLine(idconection);
            var acptationvalue = true;
            using var capture = new VideoCapture(2, VideoCaptureAPIs.DSHOW);

            int maxCameraIndex = 10; // Puedes ajustar esto según tus necesidades

            for (int i = 0; i < maxCameraIndex; i++)
            {
                using (VideoCapture capture2 = new VideoCapture(i))
                {
                    // Intentar abrir el dispositivo de captura
                    if (capture2.IsOpened())
                    {
                        string cameraName = capture2.GetBackendName().ToString(); //GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FriendlyName).ToString();
                        Console.WriteLine($"Cámara {i}: {cameraName}");
                    }
                    
                }
            }
            //for (var i = 0; i < count; i++)
            while (acptationvalue)
            {
                
                if (!capture.IsOpened()) {
                    acptationvalue = false;
                    capture.FrameWidth = 1920;
                    capture.FrameHeight = 1080;
                    capture.AutoFocus = true;

                    const int sleepTime = 10;
                }

                //using var window = new Window("capture");
                var image = new Mat();

                capture.Read(image);
                string pathsave = string.Format("{0}\\camtaked.jpg", AppDomain.CurrentDomain.BaseDirectory);
                image.SaveImage(pathsave);
                var imgretonr =image.ToBytes();

                    // Hace falta agregar una capa de seguridad de acuerdo a los roles para poder ejecutar esta linea
                    cancellationToken.ThrowIfCancellationRequested();

                yield return imgretonr;

                // Use the cancellationToken in other APIs that accept cancellation
                // tokens so the cancellation can flow down to them.
                await Task.Delay(delay);// , cancellationToken);
            }
        }
    }

    public class UpdateStatus : Hub
    {
        public async IAsyncEnumerable<dynamic> Imgupdate(
          int indexcam,
         [EnumeratorCancellation]
        CancellationToken cancellationToken)
        {
            var controlmotor = new Tim101_4_ch_inertial_motor();
            var processimgs = controlmotor.Createmosaicstepbystep(indexcam, 2, "97000001");
            await foreach (var url in processimgs)
            { 
                yield return url;
            }
            //creacion del metodo que se encarga de actualizar el estado del mapeo por imagenes en la app
            //SE PUEDE MEJORAR EL CODIGO METIENDO TODO EN LA CLASE 101_4 .... Y USANDO YIELD EN EL WHILE
        }
    }
    //public class stitcher : Hub
    //{
    //    public async IAsyncEnumerable<dynamic> Imgupdate(
    //      int indexcam,
    //     [EnumeratorCancellation]
    //    CancellationToken cancellationToken)
    //    {
    //        var controlmotor = new Tim101_4_ch_inertial_motor();
    //        var processimgs = controlmotor.CreatesticherOpencv(indexcam, 2, "97000001");
    //        await foreach (var url in processimgs)
    //        {
    //            yield return url;
    //        }
    //        //creacion del metodo que se encarga de actualizar el estado del mapeo por imagenes en la app
    //        //SE PUEDE MEJORAR EL CODIGO METIENDO TODO EN LA CLASE 101_4 .... Y USANDO YIELD EN EL WHILE
    //    }
    //}
    public class resourcesignal { 
        public string Name { get; set; }
        // public CancellationToken cancelacion { get; set; }
        public int numero { get; set; }
        public Bitmap imagen { get; set; }
       
    }
}
