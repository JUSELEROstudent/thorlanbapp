﻿using Dapper;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Services;
using Microsoft.AspNetCore.SignalR;
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
using static System.Net.Mime.MediaTypeNames;

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

        public int rows; //= i
        public int columns; // = j
        public int indexCam;
        string namefolder;

        public Mat[] image;
        public Mat[] finalimg;
        public Mat mosaic;// imagen general ya con el tamaño completo para la imagen final que laverga a todas las sub imagenes

        public TakeTour(int IndexCam, int Rows, int Columns)
        {
            rows = Rows;
            columns = Columns;
            indexCam = IndexCam;
            namefolder = Utilities.getTimeInString();

            int framewidth;
            int frameheight;

            using (var capture = new VideoCapture(indexCam, VideoCaptureAPIs.DSHOW))
            {
                frameheight = capture.FrameHeight;
                framewidth = capture.FrameWidth;
            };
            mosaic = new Mat(rows * frameheight, columns * framewidth, MatType.CV_8UC3);//mosaico final
            image = new Mat[rows];
            finalimg = new Mat[columns];
        }

       
        public async IAsyncEnumerable<dynamic> Createmosaicstepbystep(int dimMove, string kimDeviceId)
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
                // Open a connection to  devices.
                deviceconnect.Connect(kimDeviceId);
            }
            catch (Exception)
            {
                deviceconnect.Disconnect();
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
            //string namefolder = Utilities.getTimeInString();

            string fullnamefolder = CreateTour();

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
        public string CreateTour()
        {
            var currentPath = Directory.GetCurrentDirectory();
            string fullnamefolder = Path.Combine(currentPath, $"StaticFiles{Path.DirectorySeparatorChar}" + namefolder);
            bool status = Utilities.createFolder(fullnamefolder);
            using (var queryable = ConnectionSqlite.CreateConnection())
            {
                queryable.Open();
                string createTour = @$"INSERT INTO tour ( date,nameFolder, NumberX, NumberY, NumberZ, Camera) VALUES 
                                    ( '{DateTime.Now.ToString()}', '{namefolder}', '{columns}', '{rows}', '0', '{indexCam}')";
                var rowsAffected = queryable.Query(createTour);
            }

            return fullnamefolder;

        }

        public void EndStatus(string statusOfTour)
        {
            using (var queryable = ConnectionSqlite.CreateConnection())
            {
                queryable.Open();
                string createTour = @$"UPDATE tour SET endStatus='{statusOfTour}' WHERE nameFolder = '{namefolder}'";
                var rowsAffected = queryable.Query(createTour);
            }
            //throw new NotImplementedException();
        }

        public string TakeAPic(string nameFile, string path, int x, int y, int z)
        {
            Mat frame = new Mat();
            string pathsave;
            string resultadolaplace;
            string nameimage;

            using (var capture = new VideoCapture(indexCam, VideoCaptureAPIs.DSHOW))///migrar a tomar imagen
            {
                var frameheight = capture.FrameHeight;
                var framewidth = capture.FrameWidth;
                Mat mosaic = new Mat(rows * frameheight, columns * framewidth, MatType.CV_8UC3);

                if (!capture.IsOpened())
                {

                    capture.FrameWidth = 1920;
                    capture.FrameHeight = 1080;
                    capture.AutoFocus = true;

                    const int sleepTime = 10;
                }
                capture.Read(frame);
                var copyofFrame = frame;
                image[y] = frame;
                // se hcae el calculo de la place para el estado del blur 
                Mat grayresult = new Mat();
                Mat shaperesult = new Mat();
                Cv2.CvtColor(copyofFrame, grayresult, ColorConversionCodes.BGR2GRAY);
                Cv2.Laplacian(grayresult, shaperesult, MatType.CV_64F);
                Cv2.MeanStdDev(copyofFrame, out var mean, out var stddev);
                resultadolaplace = (stddev.Val0 * stddev.Val0).ToString();
                Cv2.PutText(frame, "laplacian :" + resultadolaplace, new Point(20, 30), HersheyFonts.Italic, 0.8, 1);
                Rect region = new Rect(frame.Cols * x, frame.Rows * y, frame.Cols, frame.Rows);
                frame.CopyTo(mosaic.SubMat(region));
                nameimage = $"{nameFile}{x}_{y}.jpg";
                pathsave = Path.Combine(path, nameimage);

                frame.SaveImage(pathsave);
            }

            using (var queryable = ConnectionSqlite.CreateConnection())
            {
                queryable.Open();
                string createTour = @$"INSERT INTO image ( name ,gausianVal, path, X, Y, Z,idTour)
                                    SELECT '{nameimage}',{resultadolaplace}, '{namefolder}',{x},{y},0,idTour FROM tour WHERE nameFolder = '{namefolder}'";
                var rowsAffected = queryable.Query(createTour);
            }

            return pathsave;
        }
    }
}
