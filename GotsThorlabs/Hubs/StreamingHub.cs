using GotsThorlabs.BLL;
using GotsThorlabs.Database.EntityRepo;
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
        private readonly GotsThorlabs.Interfaces.ICameraService _cameraService;

        public StreamingHub(GotsThorlabs.Interfaces.ICameraService cameraService)
        {
            _cameraService = cameraService;
        }
        public async IAsyncEnumerable<byte[]> Counter(
         int camera,
         int delay,
         [EnumeratorCancellation]
        CancellationToken cancellationToken)
        {
            var idconection = Context.ConnectionAborted;
            Console.WriteLine(idconection);
            var acptationvalue = true;

            int maxCameraIndex = 10; // Puedes ajustar esto según tus necesidades
            // cancelado porque lo que se hace a continuacion se hace en camaras directamente
            //for (int i = 0; i < maxCameraIndex; i++)
            //{
            //    using (VideoCapture capture2 = new VideoCapture(i))
            //    {
            //        // Intentar abrir el dispositivo de captura
            //        if (capture2.IsOpened())
            //        {
            //            string cameraName = capture2.GetBackendName().ToString(); //GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FriendlyName).ToString();
            //            Console.WriteLine($"Cámara {i}: {cameraName}");
            //        }
                    
            //    }
            //}
            //for (var i = 0; i < count; i++)
            while (acptationvalue)
            {
                // capture via camera service
                using var image = _cameraService?.CaptureFrame(camera) ?? new Mat();

                // if capture failed or returned empty frame, stop streaming
                if (image.Empty())
                {
                    acptationvalue = false;
                    await Task.Delay(10);
                    continue;
                }
                //El proceso de guardado queda deshabilitado por el momento
                //string pathsave = string.Format("{0}\\camtaked.jpg", AppDomain.CurrentDomain.BaseDirectory);
                //image.SaveImage(pathsave);
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
        private readonly GotsThorlabs.Database.EntityRepo.ThorlabsDbContext _db;
        private readonly GotsThorlabs.Interfaces.ICameraService _cameraService;

        public UpdateStatus(GotsThorlabs.Database.EntityRepo.ThorlabsDbContext db, GotsThorlabs.Interfaces.ICameraService cameraService)
        {
            _db = db;
            _cameraService = cameraService;
        }
        /// <summary>
        /// creacion de tour basado en el dispositivo thorlabs conectado y la groupcalibration que este valido 
        /// </summary>
        /// <param name="indexcam">no se usa esta camara ahora se usa el de grupo de calibracion </param>
        /// <param name="rows">cantidad de filas pra la creacion del mapeo </param>
        /// <param name="columns">cantidad de columnas para el mapeo </param>
        /// <param name="groupCalibrationId"> calibracion grupo que se quiere usar </param>
        /// <param name="device">dispositivo que se quire usar </param>
        /// <param name="cancellationToken">token de cancelacion de la tarea </param>
        /// <returns></returns>
        public async IAsyncEnumerable<dynamic> Imgupdate(
          int indexcam,
          int rows,
          int columns,
          Guid groupCalibrationId,
          string device,
         [EnumeratorCancellation]
        CancellationToken cancellationToken)
        {
            var controlmotor = new TakeTour(indexcam, rows, columns, _db, _cameraService);
            var processimgs = controlmotor.Createmosaicstepbystep( 2, device.Trim(), groupCalibrationId);// el Id de la camara debe venir del front
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
