using GotsThorlabs.Interfaces;
using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Database.EntityRepo.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenCvSharp;
using OpenCvSharp.Detail;
using OpenCvSharp.Internal.Vectors;
using System.Collections;
//using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Thorlabs.MotionControl.DeviceManagerCLI;
using Thorlabs.MotionControl.KCube.InertialMotorCLI;

namespace GotsThorlabs.BLL
{
    ///<summary>
    ///Clase de implementacion para el dispositivo Kim 101 de 4 canales y especificamente para el caso de un nuevo tour
    ///</summary>
    ///<remarks>
    ///crea un objeto con las utilidades para usar el dispositivo kim 101 4ch, ejecutar los metods de acuerdo al orden 
    /// 1. builddeviceslist() 2. deviceslist()
    ///</remarks>
    public class TakeTour : ITakeTour
    {
        Dictionary<int, InertialMotorStatus.MotorChannels> chanelsDevice = new Dictionary<int, InertialMotorStatus.MotorChannels>()
                {
                    {1, InertialMotorStatus.MotorChannels.Channel1},
                    {2, InertialMotorStatus.MotorChannels.Channel2},
                    {3, InertialMotorStatus.MotorChannels.Channel3},
                    {4, InertialMotorStatus.MotorChannels.Channel4}
                };

        private readonly ThorlabsDbContext _db;
        private readonly GotsThorlabs.Interfaces.ICameraService _cameraService;
        private int? _currentTourId;

        public int rows; //= i
        public int columns; // = j
        /// <summary>LocalIdentifier of the camera resolved from GroupCalibration → Camera in the DB.</summary>
        public string localIdentifier;
        string namefolder;

        public Mat[] image;
        public Mat[] finalimg;
        public Mat mosaic;// imagen general ya con el tamaño completo para la imagen final que laverga a todas las sub imagenes

        public TakeTour(string LocalIdentifier, int Rows, int Columns, ThorlabsDbContext db, GotsThorlabs.Interfaces.ICameraService cameraService)
        {
            rows = Rows;
            columns = Columns;
            localIdentifier = LocalIdentifier;
            namefolder = Utilities.getTimeInString();
            _db = db;
            _cameraService = cameraService;

            int framewidth;
            int frameheight;

            // try to obtain frame size using camera service; fallback to defaults
            try
            {
                using var frame = _cameraService?.CaptureFrame(localIdentifier);
                frameheight = frame?.Rows ?? 1080;
                framewidth = frame?.Cols ?? 1920;
            }
            catch
            {
                frameheight = 1080;
                framewidth = 1920;
            }
            mosaic = new Mat(rows * frameheight, columns * framewidth, MatType.CV_8UC3);//mosaico final
            image = new Mat[rows];
            finalimg = new Mat[columns];
        }

       
        public async IAsyncEnumerable<dynamic> Createmosaicstepbystep(int dimMove, string kimDeviceId,Guid groupCalibrationId)
        {
            var developerurl = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
            var listado = deviceslist();
            string path = Environment.CurrentDirectory;
            if (listado == null) { yield return false; }
            if (!listado.Contains(kimDeviceId)) { throw new HubException("Error al connectarse al dispositivo kim04 [ES]"); }
            var developerurl2 = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
            var urlslocals = developerurl2.Split(";");


            KCubeInertialMotor deviceconnect = KCubeInertialMotor.CreateKCubeInertialMotor(kimDeviceId);
            try
            {
                if (!deviceconnect.IsConnected)
                {
                    deviceconnect.Disconnect(true);
                }
                    // Open a connection to  devices.
                deviceconnect.Connect(kimDeviceId);
            }
            catch (Exception)
            {
                deviceconnect.Disconnect(true);
                throw new HubException("Error al establecer al dispositivo kim04 [ES]");
            }
            if (!deviceconnect.IsSettingsInitialized())
            {
                try
                {
                    deviceconnect.WaitForSettingsInitialized(500);
                }
                catch (Exception)
                {
                    //throw new HubException("Error Inicializar estado en dispositivo [ES]");
                }
            }

            deviceconnect.StartPolling(250);
            Thread.Sleep(500);
            deviceconnect.EnableDevice();
            Thread.Sleep(500);

            InertialMotorConfiguration InertialMotorConfiguration = deviceconnect.GetInertialMotorConfiguration(kimDeviceId);
            ThorlabsInertialMotorSettings currentDeviceSettings = ThorlabsInertialMotorSettings.GetSettings(InertialMotorConfiguration);

            // Set the 'Step' paramaters for the Inertia Motor and download to device
            currentDeviceSettings.Drive.Channel(chanelsDevice[1]).StepRate = 200;
            currentDeviceSettings.Drive.Channel(chanelsDevice[1]).StepAcceleration = 100;
            deviceconnect.SetSettings(currentDeviceSettings, true, true);



            Decimal newPos = deviceconnect.GetPosition(InertialMotorStatus.MotorChannels.Channel1);
            var rand = new Random();
            
            //seccion validacion de groupcalibration 
            var allCalibrations = _db.PicsCalibrations.AsNoTracking().Where(x => x.GroupCailbrationId == groupCalibrationId).ToList();
            var mostAcurateCalibration = allCalibrations.OrderBy(item => Math.Abs(item.dx)).ThenBy(item2 => Math.Abs(item2.dy)).FirstOrDefault();
            if (mostAcurateCalibration == null)
            {
                throw new Exception("No se encontró una calibración válida para el tour.");
            }
            //fin validacion groupcalibration 

            string fullnamefolder = CreateTour(mostAcurateCalibration.PicsCalibrationId);

            for (int j = 0; j < columns; j++)// posiblemente son las columnas 
            {
                bool estatusMovementA = Move_Method1(deviceconnect, chanelsDevice[1], j * 100);
                if (!estatusMovementA)
                {
                    deviceconnect.StopPolling();
                    deviceconnect.Disconnect(true);
                    throw new Exception("Error al mover el dispositivo, revisar la coneccion [ES]");
                    //yield return false;
                }

                for (int i = 0; i < rows; i++)
                {

                    Mat frame = new Mat();

                    bool estatusMovement = Move_Method1(deviceconnect, chanelsDevice[2], i * 100);
                    if (!estatusMovement)
                    {
                        deviceconnect.StopPolling();
                        deviceconnect.Disconnect(true);
                        throw new Exception("Error al mover el dispositivo, revisar la coneccion [ES]");
                        //yield return false;
                    }
                    string pathsave = TakeAPic("unitofpics", fullnamefolder, j, i, 0);
                    /////mosaic.SaveImage(pathsave);
                    var splitpathdir = pathsave.Split($"{Path.DirectorySeparatorChar}");
                    int dimpath = splitpathdir.Length;
                    var namephotounits = splitpathdir[dimpath - 1];
                    var urlunitpi = urlslocals[2] + $"/SouerceStaticFiles/{namefolder}/" + namephotounits + "?ranmd=" + rand.Next().ToString();
                    yield return urlunitpi;
                    //var imgretonr = image.ToBytes(); COMENTADA PORQUE NO SE NECESITA COMBERTIR A FRAMES
                }
                Mat mosaicv = new Mat();
                Cv2.VConcat(image, mosaicv);
                finalimg[j] = mosaicv;

                string mosaicpathv = Path.Combine(fullnamefolder, $"columnpic{j}.jpg");

                mosaicv.SaveImage(mosaicpathv);
                var namephoto1 = mosaicpathv.Split($"{Path.DirectorySeparatorChar}");
                int lengtpicpath1 = namephoto1.Length;
                var namepicstream1 = namephoto1[lengtpicpath1 - 1];
                var urlstaticfiles1 = urlslocals[2] + $"/SouerceStaticFiles/{namefolder}/" + namepicstream1 + "?ranmd=" + rand.Next().ToString();
                yield return urlstaticfiles1;
            }

            Cv2.HConcat(finalimg, mosaic);
            string mosaicpath = Path.Combine(fullnamefolder, $"HxV.jpg");
            mosaic.SaveImage(mosaicpath);
            var namephoto = mosaicpath.Split($"{Path.DirectorySeparatorChar}");
            int lengtpicpath = namephoto.Length;
            var namepicstream = namephoto[lengtpicpath - 1];

            // Tidy up and exit
            deviceconnect.StopPolling();
            deviceconnect.Disconnect(true);
            var urlstaticfiles = urlslocals[2] + $"/SouerceStaticFiles/{namefolder}/" + namepicstream + "?ranmd=" + rand.Next().ToString();
            EndStatus("succes");
            yield return urlstaticfiles;

        }
        public bool CreatesticherOpencv(int mode)/////// DEBE SER AISLADA EN SU PROPIA UNOT OF WORK
        {
            //var stitching = new emgu();

            var r1 = CreatesticherOpencv2(mode);
            var r2 = emgu.getstitcher(mode);
            bool resultado = r1 == r2 && r1 == true;
            return resultado;

        }


        ///<summary>
        ///Funcion de stitch encargada de procesar la imagenes definidas. 
        ///</summary>
        ///<return>
        ///bool return, on OK return true 
        ///</return>
        public bool CreatesticherOpencv2(int modes)/////// DEBE SER AISLADA EN SU PROPIA UNiT OF WORK
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

            var mode = modes == 0 ? OpenCvSharp.Stitcher.Mode.Scans : OpenCvSharp.Stitcher.Mode.Panorama;
            var stitched = Stitcher.Create(mode);
            var nuevo1 = new BestOf2NearestMatcher();

            var solucion = stitched.Stitch(arraisMat, output);
            var estado = solucion == Stitcher.Status.OK ? true : false;
            output.SaveImage(Path.Combine(Environment.CurrentDirectory, "StaticFiles", "openNative.jpg"));
            return estado;
        }
        ///<summary>
        ///Inicia la conexion con el dispositivo connectado 
        ///</summary>
        ///<return>
        ///Devuelve True si puede generar la lista de dispositivos connectados sin importar el tipo de dispositivo.
        ///</return>
        ///<param>
        ///Funcio sin argumentos
        ///</param>
        public bool builddeviceslist()
        {
            try
            {
                // Tell the device manager to get the list of all devices connected to the computer
                DeviceManagerCLI.BuildDeviceList();
                return true;
            }
            catch (Exception ex)
            {
                // An error occurred - see ex for details
                return false;

            }
        }
        ///<summary>
        ///se connecta para consultar los dispositivos del tipo Kim Connectados en la lista cargadas anteriormente en builddeviceslist()
        ///</summary>
        ///<return>
        ///Devuelve una lista de los dispositivos connectados.
        ///</return>
        ///<param>
        ///Funcio sin argumentos
        ///</param>
        public List<string> deviceslist()
        {
            if (builddeviceslist() == false) { return null; }
            return DeviceManagerCLI.GetDeviceList(KCubeInertialMotor.DevicePrefix_KIM101);
        }
        ///<summary>
        ///se crea el objeto que contiene el dispositivo que va a ser manejado en este caso y clase es un dispositi KIM04
        ///</summary>
        ///<return>
        ///retorna un device De tipo  KCubeInertialMotor o NULL si no se pudo crear el objeto
        ///</return>
        ///<param>
        ///nombre del dispositivo que se desea crear objeto para manejarlo
        ///</param>
        public async Task<KCubeInertialMotor> Getobjdevicekim(string devicename)
        {
            KCubeInertialMotor device = await Task.Run(() => KCubeInertialMotor.CreateKCubeInertialMotor(devicename));
            try
            {
                // Open a connection to the device.
                await Task.Run(() => device.Connect(devicename));
            }
            catch (Exception)
            {
                device.Disconnect();
                // Connection failed
                return null;
            }
            if (!device.IsSettingsInitialized())
            {
                try
                {
                    device.WaitForSettingsInitialized(1000);
                }
                catch (Exception)
                {
                    return null;
                }
            }
            return device;
        }

        ///<summary>
        ///metodo encargado del desplazamiento del motor
        ///</summary>
        ///<return>
        ///true para un movimiento correcto
        ///</return>
        ///<param>
        /// Device, canal a mover y posicion a mover.
        ///</param>

        public static bool Move_Method1(KCubeInertialMotor device, InertialMotorStatus.MotorChannels channel, int position)
        {
            // se crea la condicion para que la posicion a mover no sea igual a la acutal
            if (device.GetPosition(channel) == position) { return true; }
            try
            {
                device.MoveTo(channel, position, 6000);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// funcion encargada de crear la carpeta y la persistencia para la tabla tour
        /// </summary>
        /// <returns>retorna el nombre completo de la carpeta donde se va a guardar las imagenes </returns>
        /// <exception cref="NotImplementedException"></exception>
        public string CreateTour(Guid picsCalibrationId)
        {
            var currentPath = Directory.GetCurrentDirectory();
            string fullnamefolder = Path.Combine(currentPath, $"StaticFiles{Path.DirectorySeparatorChar}" + namefolder);
            bool status = Utilities.createFolder(fullnamefolder);

            //var allCalibrations = _db.GroupCalibrations.AsNoTracking().ToList();
            //using (var queryable = ConnectionSqlite.
            //CreateConnection())
            //{
            //    queryable.Open();
            //    string createTour = @$"INSERT INTO tour ( date,nameFolder, NumberX, NumberY, NumberZ, Camera) VALUES 
            //                        ( '{DateTime.Now.ToString()}', '{namefolder}', '{columns}', '{rows}', '0', '{indexCam}')";
            //    var rowsAffected = queryable.Query(createTour);
            //}

            var tour = new Tour
            {
                Date = DateTime.Now,
                NameFolder = namefolder,
                NumberX = columns,
                NumberY = rows,
                NumberZ = 0,
                Camera = 0, // camera identified by LocalIdentifier; int index no longer used
                PicsCalibrationId = picsCalibrationId,
            };
            _db.Tours.Add(tour);
            _db.SaveChanges();
            _currentTourId = tour.IdTour;

            return fullnamefolder;

        }

        public void EndStatus(string statusOfTour)
        {
            //using (var queryable = ConnectionSqlite.CreateConnection())
            //{
            //    queryable.Open();
            //    string createTour = @$"UPDATE tour SET endStatus='{statusOfTour}' WHERE nameFolder = '{namefolder}'";
            //    var rowsAffected = queryable.Query(createTour);
            //}

            var tourToUpdate = _currentTourId.HasValue
                ? _db.Tours.FirstOrDefault(x => x.IdTour == _currentTourId.Value)
                : _db.Tours.FirstOrDefault(x => x.NameFolder == namefolder);

            if (tourToUpdate != null)
            {
                tourToUpdate.EndStatus = statusOfTour;
                _db.SaveChanges();
            }
            //throw new NotImplementedException();
        }

        public string TakeAPic(string nameFile, string path, int x, int y, int z)
        {
            Mat frame = new Mat();
            string pathsave;
            string resultadolaplace;
            string nameimage;

            // capture frame via injected camera service resolved from GroupCalibration → Camera
            using (var captured = _cameraService?.CaptureFrame(localIdentifier))
            {
                // Clone the captured Mat so it remains valid after 'captured' is disposed
                var ownedFrame = (captured != null) ? captured.Clone() : new Mat();

                var frameheight = ownedFrame.Rows > 0 ? ownedFrame.Rows : 1080;
                var framewidth = ownedFrame.Cols > 0 ? ownedFrame.Cols : 1920;
                using (var mosaic = new Mat(rows * frameheight, columns * framewidth, MatType.CV_8UC3))
                {
                    // Dispose any previous stored frame at this slot to avoid leaks
                    image[y]?.Dispose();
                    image[y] = ownedFrame;

                    // se hcae el calculo de la place para el estado del blur 
                    using var grayresult = new Mat();
                    using var shaperesult = new Mat();
                    Cv2.CvtColor(ownedFrame, grayresult, ColorConversionCodes.BGR2GRAY);
                    Cv2.Laplacian(grayresult, shaperesult, MatType.CV_64F);
                    Cv2.MeanStdDev(ownedFrame, out var mean, out var stddev);
                    resultadolaplace = (stddev.Val0 * stddev.Val0).ToString();

                    Rect region = new Rect(ownedFrame.Cols * x, ownedFrame.Rows * y, ownedFrame.Cols, ownedFrame.Rows);
                    ownedFrame.CopyTo(mosaic.SubMat(region));
                    nameimage = $"{nameFile}{x}_{y}.jpg";
                    pathsave = Path.Combine(path, nameimage);

                    ownedFrame.SaveImage(pathsave);
                }
            }

            //using (var queryable = ConnectionSqlite.CreateConnection())
            //{
            //    queryable.Open();
            //    string createTour = @$"INSERT INTO image ( name ,gausianVal, path, X, Y, Z,idTour)
            //                        SELECT '{nameimage}',{resultadolaplace}, '{namefolder}',{x},{y},0,idTour FROM tour WHERE nameFolder = '{namefolder}'";
            //    var rowsAffected = queryable.Query(createTour);
            //}

            var resolvedTourId = _currentTourId ?? _db.Tours
                .Where(x => x.NameFolder == namefolder)
                .Select(x => (int?)x.IdTour)
                .FirstOrDefault();

            if (resolvedTourId.HasValue)
            {
                _db.Images.Add(new GotsThorlabs.Database.EntityRepo.Entities.Image
                {
                    Name = nameimage,
                    GausianVal = double.TryParse(resultadolaplace, out var blurValue) ? blurValue : null,
                    Path = namefolder,
                    X = x,
                    Y = y,
                    Z = 0,
                    IdTour = resolvedTourId.Value
                });
                _db.SaveChanges();
            }

            return pathsave;
        }
    }
}
