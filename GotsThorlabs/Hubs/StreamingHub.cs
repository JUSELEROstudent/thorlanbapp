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
            using var capture = new VideoCapture(0, VideoCaptureAPIs.DSHOW);
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
        public List<string> deviceslist()
        {
            if (builddeviceslist() == false) { return null; }
            return DeviceManagerCLI.GetDeviceList(KCubeInertialMotor.DevicePrefix_KIM101);
        }
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
        public async IAsyncEnumerable<dynamic> Imgupdate(
         [EnumeratorCancellation]
        CancellationToken cancellationToken)
        {
            //creacion del metodo que se encarga de actualizar el estado del mapeo por imagenes en la app
            //SE PUEDE MEJORAR EL CODIGO METIENDO TODO EN LA CLASE 101_4 .... Y USANDO YIELD EN EL WHILE

            Dictionary<int, InertialMotorStatus.MotorChannels> chanelsDevice = new Dictionary<int, InertialMotorStatus.MotorChannels>()
                {
                    {1, InertialMotorStatus.MotorChannels.Channel1},
                    {2, InertialMotorStatus.MotorChannels.Channel2},
                    {3, InertialMotorStatus.MotorChannels.Channel3},
                    {4, InertialMotorStatus.MotorChannels.Channel4}
                };
            var developerurl = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
            var listado = deviceslist();
            if (listado == null) { yield return false; }
            var developerurl2 = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
            var urlslocals = developerurl2.Split(";");
            //KCubeInertialMotor deviceconnect = await Getobjdevicekim(listado[0]);
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
                        yield return false;
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
                string mosaicpathv = string.Format("{0}\\camtakedV{1}.jpg", "C:\\Users\\cocuy\\OneDrive\\Escritorio\\thorlabs\\GotsThorlabs\\GotsThorlabs\\StaticFiles", j);
                mosaicv.SaveImage(mosaicpathv);
                var namephoto1 = mosaicpathv.Split("\\");
                int lengtpicpath1 = namephoto1.Length;
                var namepicstream1 = namephoto1[lengtpicpath1 - 1];
                var urlstaticfiles1 = urlslocals[0] + "/SouerceStaticFiles/" + namepicstream1;
                yield return urlstaticfiles1;
            }
            Mat mosaic = new Mat();
            Cv2.HConcat(finalimg, mosaic);
            string mosaicpath = string.Format("{0}\\HxV{1}.jpg", "C:\\Users\\cocuy\\OneDrive\\Escritorio\\thorlabs\\GotsThorlabs\\GotsThorlabs\\StaticFiles", "mosaic");
            mosaic.SaveImage(mosaicpath);
            var namephoto = mosaicpath.Split("\\");
            int lengtpicpath = namephoto.Length;
            var namepicstream = namephoto[lengtpicpath - 1];

            // Tidy up and exit
            deviceconnect.StopPolling();
            deviceconnect.Disconnect(true);
            var urlstaticfiles = urlslocals[0] + "/SouerceStaticFiles/" + namepicstream;
            yield return urlstaticfiles;
        }
    }

    public class resourcesignal { 
        public string Name { get; set; }
        // public CancellationToken cancelacion { get; set; }
        public int numero { get; set; }
        public Bitmap imagen { get; set; }
       
    }
}
