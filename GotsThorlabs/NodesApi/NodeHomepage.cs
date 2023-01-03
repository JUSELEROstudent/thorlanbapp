using Thorlabs.MotionControl.DeviceManagerCLI;
using Thorlabs.MotionControl.KCube.InertialMotorCLI;
using Thorlabs.MotionControl.GenericMotorCLI;
using AForge.Video.DirectShow;
using AForge.Video;
using System.Drawing;
using OpenCvSharp;
using System.Diagnostics;
using System.Net;
using Thorlabs.MotionControl.Tools.Common;
using Thorlabs.MotionControl.Tools.Logging;


namespace GotsThorlabs.NodesApi
{
    public class NodeHomepage
    {
        Decimal positionchanel = 0;
        FrameSource frameSource;
        public static void Move_Method1(KCubeInertialMotor device, InertialMotorStatus.MotorChannels channel, int position)
        {
            try
            {
                Console.WriteLine("Moving Device to {0}", position);
                device.MoveTo(channel, position, 60000);
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to move to position");
                Console.ReadKey();
                return;
            }
            Console.WriteLine("Device Moved");
        }

        public NodeHomepage(WebApplication App)
        {
            App.MapGet("/home/devices", async () =>
            {
                // SimulationManager.Instance.InitializeSimulations();
                try
                {
                    // Tell the device manager to get the list of all devices connected to the computer
                    DeviceManagerCLI.BuildDeviceList();
                }
                catch (Exception ex)
                {
                    // An error occurred - see ex for details
                    Console.WriteLine("Exception raised by BuildDeviceList {0}", ex);
                    Console.ReadKey();

                }
                List<string> serialNumbers = DeviceManagerCLI.GetDeviceList(KCubeInertialMotor.DevicePrefix_KIM101);
                //CargaDispositivos();
                // Readcameras
                //SimulationManager.Instance.UninitializeSimulations();

                KCubeInertialMotor device = KCubeInertialMotor.CreateKCubeInertialMotor(serialNumbers[0]);// nunca misrar esta varable linea 
                if (device == null)
                {
                    // An error occured

                }

                // Open a connection to the device.
                try
                {
                    device.Connect(serialNumbers[0]);
                }
                catch (Exception)
                {
                    // Connection failed
                    serialNumbers.Add("Failed to open device {0}");
                    return serialNumbers;
                }
                if (!device.IsSettingsInitialized())
                {
                    try
                    {
                        device.WaitForSettingsInitialized(5000);
                    }
                    catch (Exception)
                    {
                        serialNumbers.Add("Settings failed to initialize");
                    }
                }
                DeviceInfo deviceInfo = device.GetDeviceInfo();
                Console.WriteLine("Device {0} = {1}", deviceInfo.SerialNumber, deviceInfo.Name);
                device.StartPolling(250);
                Thread.Sleep(500);
                device.EnableDevice();
                Thread.Sleep(500);

                InertialMotorConfiguration InertialMotorConfiguration = device.GetInertialMotorConfiguration(serialNumbers[0]);
                ThorlabsInertialMotorSettings currentDeviceSettings = ThorlabsInertialMotorSettings.GetSettings(InertialMotorConfiguration);

                // Set the 'Step' paramaters for the Inertia Motor and download to device
                currentDeviceSettings.Drive.Channel(InertialMotorStatus.MotorChannels.Channel1).StepRate = 500;
                currentDeviceSettings.Drive.Channel(InertialMotorStatus.MotorChannels.Channel1).StepAcceleration = 100000;
                device.SetSettings(currentDeviceSettings, true, true);

                // Zero the device
                //device.SetPositionAs(InertialMotorStatus.MotorChannels.Channel1, 0);
                positionchanel = device.GetPosition(InertialMotorStatus.MotorChannels.Channel1);
                int position =  100;
                position = positionchanel == position ? position + 100 : 100;

                Move_Method1(device, InertialMotorStatus.MotorChannels.Channel1, position);
                serialNumbers.Add("se ha movido");
                // or
                // Move_Method2(device, InertialMotorStatus.MotorChannels.Channel1, position);

                Decimal newPos = device.GetPosition(InertialMotorStatus.MotorChannels.Channel1);
                Console.WriteLine("Device Moved to {0}", newPos);

                // Tidy up and exit
                device.StopPolling();
                device.Disconnect(true);



                return serialNumbers;
            });
        } 

        public bool builddeviceslist ()
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
                Console.WriteLine("Exception raised by BuildDeviceList {0}", ex);
                Console.ReadKey();
                return false;

            }
        }

        //public void Readcameras() 
        //{
        //    using var capture = new VideoCapture(0, VideoCaptureAPIs.DSHOW);
        //    if (!capture.IsOpened())
        //        return;

        //    capture.FrameWidth = 1920;
        //    capture.FrameHeight = 1280;
        //    capture.AutoFocus = true;

        //    const int sleepTime = 10;

        //    using var window = new Window("capture");
        //    var image = new Mat();

        //    capture.Read(image);
        //    string pathsave = string.Format("{0}\\camtaked.jpg", AppDomain.CurrentDomain.BaseDirectory);
        //    image.SaveImage(pathsave);

        //    while (true)
        //    {
        //        capture.Read(image);
        //        if (image.Empty())
        //            break;

        //        window.ShowImage(image);
        //        int c = Cv2.WaitKey(sleepTime);
        //        if (c >= 0)
        //        {
        //            break;
        //        }
        //    }
        //}

    }
}
