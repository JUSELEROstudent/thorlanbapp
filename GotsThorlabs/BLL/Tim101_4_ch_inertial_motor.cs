using OpenCvSharp;
using System.Threading;
using Thorlabs.MotionControl.DeviceManagerCLI;
using Thorlabs.MotionControl.KCube.InertialMotorCLI;

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

        public async Task<bool> Createimagemosaic() 
        {
            var listado = deviceslist();
            if (listado == null) { return false; }
            //KCubeInertialMotor deviceconnect = await Getobjdevicekim(listado[0]);
            KCubeInertialMotor deviceconnect = await Task.Run(() => KCubeInertialMotor.CreateKCubeInertialMotor(listado[0]));
            try
            {
                // Open a connection to the device.
                await Task.Run(() => deviceconnect.Connect(listado[0]));
            }
            catch (Exception)
            {
                deviceconnect.Disconnect();
                // Connection failed
                //return null;
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
            
            for (int i = 0;i < image.Length; i++)
            {
                Mat frame = new Mat();

                bool estatusMovement = Move_Method1(deviceconnect, chanelsDevice[1], i*100);
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
                string pathsave = string.Format("{0}\\camtaked{1}.jpg", "C:\\Users\\cocuy\\AppData\\Local\\Temp\\tempimg", i*100);
                frame.SaveImage(pathsave);
                //var imgretonr = image.ToBytes(); COMENTADA PORQUE NO SE NECESITA COMBERTIR A FRAMES
            }
            Mat mosaic = new Mat();
            Cv2.VConcat(image, mosaic);
            string mosaicpath = string.Format("{0}\\camtaked{1}.jpg", "C:\\Users\\cocuy\\AppData\\Local\\Temp\\tempimg", "mosaic");
            mosaic.SaveImage(mosaicpath);

            // Tidy up and exit
            deviceconnect.StopPolling();
            deviceconnect.Disconnect(true);

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
        public async Task<dynamic> GetStatusChannels()
        {
            var listado = deviceslist();
            if (listado == null) { return false; }
            var deviceconnect = await Getobjdevicekim(listado[0]);
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

            return (new { channel1 = positionchanel1, channel2 = positionchanel2, channel3 = positionchanel3, channel4 = positionchanel4 });
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
                device.MoveTo(channel, position, 60000);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
    }
}
