using OpenCvSharp;
using OpenCvSharp.Internal.Vectors;
using System.Collections;
//using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using Thorlabs.MotionControl.DeviceManagerCLI;
using Thorlabs.MotionControl.KCube.InertialMotorCLI;
using static System.Net.Mime.MediaTypeNames;

namespace GotsThorlabs.BLL
{
    ///<summary>
    ///Clase de implementacion para el dispositivo Kim 101 de 4 canales
    ///</summary>
    ///<remarks>
    ///crea un objeto con las utilidades para usar el dispositivo kim 101 4ch, ejecutar los metods de acuerdo al orden 
    /// 1. builddeviceslist() 2. deviceslist()
    ///</remarks>
    public class Tim101_4_ch_inertial_motor
    {
        Dictionary<int, InertialMotorStatus.MotorChannels> chanelsDevice = new Dictionary<int, InertialMotorStatus.MotorChannels>()
                {
                    {1, InertialMotorStatus.MotorChannels.Channel1},
                    {2, InertialMotorStatus.MotorChannels.Channel2},
                    {3, InertialMotorStatus.MotorChannels.Channel3},
                    {4, InertialMotorStatus.MotorChannels.Channel4}
                };

        public dynamic Createimagemosaic()
        {
            var listado = deviceslist();
            if (listado == null) { return false; }
            //KCubeInertialMotor deviceconnect = await Getobjdevicekim(listado[0]);
            KCubeInertialMotor deviceconnect =  KCubeInertialMotor.CreateKCubeInertialMotor(listado[0]);
            try
            {
                // Open a connection to the device.
                 deviceconnect.Connect(listado[0]);
            }
            catch (Exception)
            {
                deviceconnect.Disconnect();
            }
            if (!deviceconnect.IsSettingsInitialized())
            {
                try
                {
                    deviceconnect.WaitForSettingsInitialized(1000);
                }
                catch (Exception)
                {
                }
            }
            deviceconnect.StartPolling(250);
            Thread.Sleep(500);
            deviceconnect.EnableDevice();
            Thread.Sleep(500);

            InertialMotorConfiguration InertialMotorConfiguration = deviceconnect.GetInertialMotorConfiguration(listado[0]);
            ThorlabsInertialMotorSettings currentDeviceSettings = ThorlabsInertialMotorSettings.GetSettings(InertialMotorConfiguration);

            // Set the 'Step' paramaters for the Inertia Motor and download to device
            currentDeviceSettings.Drive.Channel(chanelsDevice[1]).StepRate = 200;
            currentDeviceSettings.Drive.Channel(chanelsDevice[1]).StepAcceleration = 100;
            deviceconnect.SetSettings(currentDeviceSettings, true, true);

            // or
            // Move_Method2(device, InertialMotorStatus.MotorChannels.Channel1, position);

            Decimal newPos = deviceconnect.GetPosition(InertialMotorStatus.MotorChannels.Channel1);
            // SECCION TOMA DE IMAGENES
            var acptationvalue = true;
            using var capture = new VideoCapture(0, VideoCaptureAPIs.DSHOW);
            Mat[] image = new Mat[10];
            Mat[] finalimg = new Mat[3];
            for (int j = 0; j < finalimg.Length; j++)
            { 
                for (int i = 0; i < image.Length; i++)
                {
                    Mat frame = new Mat();

                    bool estatusMovement = Move_Method1(deviceconnect, chanelsDevice[1], i * 100);
                    if (!estatusMovement)
                    {
                        deviceconnect.StopPolling();
                        deviceconnect.Disconnect(true);
                        return false;
                    }

                    if (!capture.IsOpened())
                    {
                        acptationvalue = false;
                        capture.FrameWidth = 1920;
                        capture.FrameHeight = 1080;
                        capture.AutoFocus = true;

                        const int sleepTime = 10;
                    }

                    //using var window = new Window("capture");
                    //var image = new Mat();

                    capture.Read(frame);
                    image[i] = frame;
                    string pathsave = string.Format("{0}\\camtaked{1}.jpg", "C:\\Users\\cocuy\\AppData\\Local\\Temp\\tempimg", i * 100);
                    frame.SaveImage(pathsave);
                    //var imgretonr = image.ToBytes(); COMENTADA PORQUE NO SE NECESITA COMBERTIR A FRAMES
                }
                Mat mosaicv = new Mat();
                Cv2.VConcat(image, mosaicv);
                finalimg[j] = mosaicv;
                string mosaicpathv = string.Format("{0}\\camtakedV{1}.jpg", "C:\\Users\\cocuy\\AppData\\Local\\Temp\\tempimg", j);
                mosaicv.SaveImage(mosaicpathv);
            }
            Mat mosaic = new Mat();
            Cv2.HConcat(finalimg, mosaic);
            string mosaicpath = string.Format("{0}\\HxV{1}.jpg", "C:\\Users\\cocuy\\AppData\\Local\\Temp\\tempimg", "mosaic");
            mosaic.SaveImage(mosaicpath);

            // Tidy up and exit
            deviceconnect.StopPolling();
            deviceconnect.Disconnect(true);

            return true;
        }
        ///<summary>
        ///Metodo encargado de devolver paso a paso la creacion del mosaico 
        ///</summary>
        ///<param name="indexCam">
        ///indice de camara que se desea usar en el mapeado
        ///</param>
        ///<param name="dimMove">
        ///tipo de movimiento que se desea ejecutar sea 1D, 2D, 3D  se usa para validarse que la cantidad de motores permita el tipo de recorrido deseado
        ///</param>
        ///<remarks>
        ///devuelve la url de la ubicacion en el servidor de la imagen actual del mapeo
        ///</remarks>
        public async IAsyncEnumerable<dynamic> Createmosaicstepbystep(int indexCam, int dimMove, string kimDeviceId)
        {
            var developerurl = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
            var listado = deviceslist();
            string path = Directory.GetCurrentDirectory();
            if (listado == null) { yield return false; }
            if (!listado.Contains(kimDeviceId)) { yield return false; }
            var developerurl2 = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
            var urlslocals = developerurl2.Split(";");
            //KCubeInertialMotor deviceconnect = await Getobjdevicekim(listado[0]);
            //List<KCubeInertialMotor> inertialdevicesconnections = new List<KCubeInertialMotor>();

            KCubeInertialMotor deviceconnect = KCubeInertialMotor.CreateKCubeInertialMotor(kimDeviceId);
            try
            {
                // Open a connection to  devices.
                deviceconnect.Connect(kimDeviceId);
            }
            catch (Exception)
            {
                deviceconnect.Disconnect();
            }
            if (!deviceconnect.IsSettingsInitialized())
            {
                try
                {
                    deviceconnect.WaitForSettingsInitialized(500);
                }
                catch (Exception)
                {
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

            // or
            // Move_Method2(device, InertialMotorStatus.MotorChannels.Channel1, position);

            Decimal newPos = deviceconnect.GetPosition(InertialMotorStatus.MotorChannels.Channel1);
            // SECCION TOMA DE IMAGENES
            var currentPath = Directory.GetCurrentDirectory();
            var rowshmosaic = 10;
            var columnmosaic = 5;
            var acptationvalue = true;
            using var capture = new VideoCapture(indexCam, VideoCaptureAPIs.DSHOW);
            var frameheight = capture.FrameHeight;
            var framewidth = capture.FrameWidth;
            Mat mosaic = new Mat(rowshmosaic*frameheight, columnmosaic*framewidth, MatType.CV_8UC3);
            Mat[] image = new Mat[rowshmosaic];
            Mat[] finalimg = new Mat[columnmosaic];
            var rand = new Random();

            // Creacion de carpeta y nombre de archivo CSV con la clase encargada de gestionar el archivo
            string namefile = Utilities.getTimeInString();
            string namefolder = currentPath + "\\StaticFiles\\" + namefile;
            bool createfolder = Utilities.createFolder(namefolder);
            CollageGestor recordTrackCollage = new CollageGestor(namefile, namefolder);

            for (int j = 0; j < finalimg.Length; j++)// all of the FOR statment is one of the dimensions of the movement
            {
                bool estatusMovementA = Move_Method1(deviceconnect, chanelsDevice[1], j * 100);
                if (!estatusMovementA)
                {
                    deviceconnect.StopPolling();
                    deviceconnect.Disconnect(true);
                    yield return false;
                }

                for (int i = 0; i < image.Length; i++)
                {

                    Mat frame = new Mat();

                    bool estatusMovement = Move_Method1(deviceconnect, chanelsDevice[2], i * 100);
                    if (!estatusMovement)
                    {
                        deviceconnect.StopPolling();
                        deviceconnect.Disconnect(true);
                        yield return false;
                    }

                    recordTrackCollage.saveStepDeviceInCsv((j * 100).ToString(), (i * 100).ToString());

                    if (!capture.IsOpened())
                    {
                        acptationvalue = false;
                        capture.FrameWidth = 1920;
                        capture.FrameHeight = 1080;
                        capture.AutoFocus = true;

                        const int sleepTime = 10;
                    }
                    // se van a hacer dos verciones de la imagen que se van a guardar en la carpeta de archivos estaticos uno se enviara todo el tiempo el otro se enviara a lfinalizar



                    capture.Read(frame);
                    image[i] = frame;
                    Mat copyofFrame = new Mat();
                    copyofFrame = frame;
                        // se hcae el calculo de la place para el estado del blur 
                    Mat grayresult = new Mat();
                    Mat shaperesult = new Mat();
                    Cv2.CvtColor(copyofFrame, grayresult, ColorConversionCodes.BGR2GRAY);
                    Cv2.Laplacian(grayresult, shaperesult, MatType.CV_64F); 
                    //Cv2.ImShow("Imagen", shaperesult);
                    //Cv2.WaitKey(0);
                    Cv2.MeanStdDev(copyofFrame, out var mean, out var stddev);
                    var resultadolaplace = (stddev.Val0 * stddev.Val0).ToString();

                    Cv2.PutText(frame, "laplacian :" + resultadolaplace, new Point(20, 30), HersheyFonts.Italic, 0.8,1);


                    Rect region = new Rect(frame.Cols*j, frame.Rows * i, frame.Cols, frame.Rows);
                    frame.CopyTo(mosaic.SubMat(region));
                    string pathsave = string.Format("{0}\\unitofpics{1}.jpg", currentPath + "\\StaticFiles", i);
                    mosaic.SaveImage(pathsave);
                    var splitpathdir = pathsave.Split("\\");
                    int dimpath = splitpathdir.Length;
                    var namephotounits = splitpathdir[dimpath - 1];
                    var urlunitpi = "https://192.168.1.37:4040" + "/SouerceStaticFiles/" + namephotounits + "?ranmd=" + rand.Next().ToString();
                    yield return urlunitpi;
                    //var imgretonr = image.ToBytes(); COMENTADA PORQUE NO SE NECESITA COMBERTIR A FRAMES
                }
                Mat mosaicv = new Mat();
                Cv2.VConcat(image, mosaicv);
                finalimg[j] = mosaicv;
                Point pts1 = new Point(100,27);
                Point pts2 = new Point(350,600);
                Cv2.Rectangle(mosaicv, pts1, pts2,new Scalar(0, 0, 255), 10);

                string mosaicpathv = string.Format("{0}\\camtakedV{1}.jpg", currentPath + "\\StaticFiles", j);
                mosaicv.SaveImage(mosaicpathv);
                var namephoto1 = mosaicpathv.Split("\\");
                int lengtpicpath1 = namephoto1.Length;
                var namepicstream1 = namephoto1[lengtpicpath1 - 1];
                var urlstaticfiles1 = urlslocals[0] + "/SouerceStaticFiles/" + namepicstream1 + "?ranmd=" + rand.Next().ToString();
                yield return urlstaticfiles1;
            }

            Cv2.HConcat(finalimg, mosaic);
            string mosaicpath = string.Format("{0}\\HxV{1}.jpg", currentPath + "\\StaticFiles", "mosaic");
            mosaic.SaveImage(mosaicpath);
            var namephoto = mosaicpath.Split("\\");
            int lengtpicpath = namephoto.Length;
            var namepicstream = namephoto[lengtpicpath - 1];

            // Tidy up and exit
            deviceconnect.StopPolling();
            deviceconnect.Disconnect(true);
            var urlstaticfiles = urlslocals[0] + "/SouerceStaticFiles/" + namepicstream + "?ranmd=" + rand.Next().ToString();
            yield return urlstaticfiles;

        }
        public  bool CreatesticherOpencv()
        {
            var stitching = new emgu();

            CreatesticherOpencv2();
            return stitching.getstitcher(); 

        }
        public bool CreatesticherOpencv2()
        {
            var output = new Mat();
            var img1 = new Mat("C:\\Users\\cocuy\\OneDrive\\Escritorio\\thorlabs\\GotsThorlabs\\GotsThorlabs\\StaticFiles\\stitchy1.png");
            //var img1 = new Mat("C:\\Users\\cocuy\\OneDrive\\Escritorio\\thorlabs\\GotsThorlabs\\GotsThorlabs\\StaticFiles\\stitche1.jpg", OpenCvSharp.ImreadModes.Color);
            var img2 = new Mat("C:\\Users\\cocuy\\OneDrive\\Escritorio\\thorlabs\\GotsThorlabs\\GotsThorlabs\\StaticFiles\\stitchy2.png");
            var img3 = new Mat("C:\\Users\\cocuy\\OneDrive\\Escritorio\\thorlabs\\GotsThorlabs\\GotsThorlabs\\StaticFiles\\stitchy3.png");
            Mat[] arraisMat = new Mat[] { img1, img2, img3 };
            var mode = OpenCvSharp.Stitcher.Mode.Scans;
            var stitched = Stitcher.Create(mode);
            stitched.Stitch(arraisMat, output);
            output.SaveImage("C:\\Users\\cocuy\\OneDrive\\Escritorio\\thorlabs\\GotsThorlabs\\GotsThorlabs\\StaticFiles\\openNative.png");
            return true;
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
                // Console.WriteLine("eccepcion Creada en el paso BuildDeviceList {0}", ex); see ilimina para no molestar la consola
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
        ///Funcion encargada de devolver el estado actual de cada canal en el dispositivo
        ///</summary>
        ///<return>
        ///retorna un objeto dinamico que contiene los canales del dispositivo y posicion
        ///</return>
        ///<param>
        /// VOID
        ///</param>
        public dynamic GetStatusChannels(string channeltarget)
        {
            var listado = deviceslist();
            //var isonlist = from itemm in listado where itemm.ToString() == channeltarget select itemm;
            var isonlist = listado.Any(x => x == channeltarget);
            if (isonlist ==false) { return false;  }

            KCubeInertialMotor deviceconnect = KCubeInertialMotor.CreateKCubeInertialMotor(listado[0]);
            try
            {
                // Open a connection to the device.
                deviceconnect.Connect(listado[0]);
            }
            catch (Exception)
            {
                deviceconnect.Disconnect();
            }
            if (!deviceconnect.IsSettingsInitialized())
            {
                try
                {
                    deviceconnect.WaitForSettingsInitialized(1000);
                }
                catch (Exception)
                {
                }
            }
            deviceconnect.StartPolling(250);
            Thread.Sleep(500);
            deviceconnect.EnableDevice();
            Thread.Sleep(500);
            decimal positionchanel1 = deviceconnect.GetPosition(chanelsDevice[1]);
            decimal positionchanel2 = deviceconnect.GetPosition(chanelsDevice[2]);
            decimal positionchanel3 = deviceconnect.GetPosition(chanelsDevice[3]);
            decimal positionchanel4 = deviceconnect.GetPosition(chanelsDevice[4]);
            deviceconnect.StopPolling();
            deviceconnect.Disconnect(true);
            var  currentdir = Environment.CurrentDirectory;

            return (new { device = listado[0] , channelsa = new { channel1 = positionchanel1, channel2 = positionchanel2, channel3 = positionchanel3, channel4 = positionchanel4 } });
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
    }
}
