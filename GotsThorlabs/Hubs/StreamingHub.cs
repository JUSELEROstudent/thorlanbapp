using GotsThorlabs.BLL;
using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenCvSharp;
using System.Drawing;
using System.Runtime.CompilerServices;
using Thorlabs.MotionControl.DeviceManagerCLI;
using Thorlabs.MotionControl.KCube.InertialMotorCLI;

namespace GotsThorlabs.Hubs
{
    public class StreamingHub : Hub
    {
        private readonly CameraServiceFactory _cameraFactory;
        private readonly ThorlabsDbContext _db;

        public StreamingHub(CameraServiceFactory cameraFactory, ThorlabsDbContext db)
        {
            _cameraFactory = cameraFactory;
            _db = db;
        }

        /// <summary>
        /// Live preview stream.
        /// cameraName: name of the camera record in DB — used to resolve the correct driver.
        ///             Falls back to "generic" if no record is found.
        /// </summary>
        public async IAsyncEnumerable<byte[]> Counter(
         int camera,
         int delay,
         string? cameraName,
         [EnumeratorCancellation]
        CancellationToken cancellationToken)
        {
            var idconection = Context.ConnectionAborted;
            Console.WriteLine(idconection + "----here is"); 

            // Provisional: resolve driver from DB camera record by name.
            // TODO: replace with a more robust selection mechanism.
            var driverType = "generic";
            if (!string.IsNullOrWhiteSpace(cameraName))
            {
                var cameraRecord = await _db.Cameras
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == cameraName.Trim().ToLower());
                if (cameraRecord != null)
                    driverType = cameraRecord.DriverType;
            }

            var cameraService = _cameraFactory.GetService(driverType);

            // Resolve the device's LocalIdentifier from the DB record.
            // Falls back to the raw camera int as string for generic drivers.
            var localIdentifier = camera.ToString();
            if (!string.IsNullOrWhiteSpace(cameraName))
            {
                var record = await _db.Cameras
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == cameraName.Trim().ToLower());
                if (record != null && !string.IsNullOrWhiteSpace(record.LocalIdentifier))
                    localIdentifier = record.LocalIdentifier;
            }

            var acptationvalue = true;

            while (acptationvalue)
            {
                // capture via selected camera service (blocking per device)
                using var image = cameraService.CaptureFrame(localIdentifier) ?? new Mat();

                if (image.Empty())
                {
                    acptationvalue = false;
                    await Task.Delay(10);
                    continue;
                }
                var imgretonr = image.ToBytes();

                cancellationToken.ThrowIfCancellationRequested();

                yield return imgretonr;

                await Task.Delay(delay);
            }
        }
    }

    public class UpdateStatus : Hub
    {
        private readonly ThorlabsDbContext _db;
        private readonly CameraServiceFactory _cameraFactory;

        public UpdateStatus(ThorlabsDbContext db, CameraServiceFactory cameraFactory)
        {
            _db = db;
            _cameraFactory = cameraFactory;
        }

        /// <summary>
        /// Starts a mosaic tour using the camera associated with the groupCalibration.
        /// The camera's DriverType field determines which capture service is used.
        /// </summary>
        public async IAsyncEnumerable<dynamic> Imgupdate(
          int indexcam,
          int rows,
          int columns,
          Guid groupCalibrationId,
          string device,
         [EnumeratorCancellation]
        CancellationToken cancellationToken)
        {
            // Resolve camera driver from the group calibration -> camera entity
            var groupCalibration = await _db.GroupCalibrations
                .AsNoTracking()
                .Include(g => g.Camera)
                .FirstOrDefaultAsync(g => g.GroupCailbrationId == groupCalibrationId);

            var driverType = groupCalibration?.Camera?.DriverType ?? "generic";
            var cameraService = _cameraFactory.GetService(driverType);

            var controlmotor = new TakeTour(indexcam, rows, columns, _db, cameraService);
            var processimgs = controlmotor.Createmosaicstepbystep(2, device.Trim(), groupCalibrationId);
            await foreach (var url in processimgs)
            {
                yield return url;
            }
        }
    }
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
