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
using Microsoft.AspNetCore.Mvc;
using static Thorlabs.MotionControl.KCube.InertialMotorCLI.InertialMotorStatus;

namespace GotsThorlabs.NodesApi
{
    public class NodeHomepage
    {
        Decimal positionchanel = 0;
        FrameSource frameSource;
        public static bool Move_Method1(KCubeInertialMotor device, InertialMotorStatus.MotorChannels channel, int position)
        {
            try
            {
                Console.WriteLine("Moving Device to {0}", position);
                device.MoveTo(channel, position, 60000);

            }
            catch (Exception)
            {
                
                return false;
            }
            Console.WriteLine("Device Moved");
            return true;
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
                Console.WriteLine("eccepcion Creada en el paso BuildDeviceList {0}", ex);
                return false;

            }
        }

        public NodeHomepage(WebApplication App)
        {
            App.MapPost("/movedevice",  ([FromBody]ObjMovement movestosite) =>
            {
                
                // SimulationManager.Instance.InitializeSimulations();
                if (!builddeviceslist()) return new List<string> { "No se ha podido iniciar la lista de dispositivos" };
                List<string>  serialNumbers = DeviceManagerCLI.GetDeviceList(KCubeInertialMotor.DevicePrefix_KIM101);

                KCubeInertialMotor device = KCubeInertialMotor.CreateKCubeInertialMotor(movestosite.deviceId);// nunca misrar esta varable linea 
                if (device == null)
                {
                    // An error occured
                    serialNumbers.Add("no existe un componente conectado");

                }
                // Open a connection to the device.
                try
                {
                    device.Connect(movestosite.deviceId);
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
                        device.WaitForSettingsInitialized(1000);
                    }
                    catch (Exception)
                    {
                        serialNumbers.Add("Settings failed to initialize"); // esta pasando todo el timepo aca
                    }
                }
                DeviceInfo deviceInfo = device.GetDeviceInfo();
                Console.WriteLine("Device {0} = {1}", deviceInfo.SerialNumber, deviceInfo.Name);
                device.StartPolling(250);
                Thread.Sleep(500);
                device.EnableDevice();
                Thread.Sleep(500);

                InertialMotorConfiguration InertialMotorConfiguration = device.GetInertialMotorConfiguration(movestosite.deviceId);
                ThorlabsInertialMotorSettings currentDeviceSettings = ThorlabsInertialMotorSettings.GetSettings(InertialMotorConfiguration);

                Dictionary<int, InertialMotorStatus.MotorChannels> chanelsDevice = new Dictionary<int, InertialMotorStatus.MotorChannels>()
                {
                    {1, InertialMotorStatus.MotorChannels.Channel1},
                    {2, InertialMotorStatus.MotorChannels.Channel2},
                    {3, InertialMotorStatus.MotorChannels.Channel3},
                    {4, InertialMotorStatus.MotorChannels.Channel4}
                };

                // Set the 'Step' paramaters for the Inertia Motor and download to device
                currentDeviceSettings.Drive.Channel(chanelsDevice[movestosite.chaneltomove]).StepRate = movestosite.steprate;
                currentDeviceSettings.Drive.Channel(chanelsDevice[movestosite.chaneltomove]).StepAcceleration = movestosite.stepaceleration;
                device.SetSettings(currentDeviceSettings, true, true);

                positionchanel = device.GetPosition(InertialMotorStatus.MotorChannels.Channel1);
                int position = movestosite.moveto;
                // position = positionchanel == position ? position + 100 : 100;

                bool estatusMovement = Move_Method1(device, chanelsDevice[movestosite.chaneltomove], position);
                if (!estatusMovement) return new List<string> { "Ocurrio un erro al mover el dispositvo" };
                // or
                // Move_Method2(device, InertialMotorStatus.MotorChannels.Channel1, position);

                Decimal newPos = device.GetPosition(InertialMotorStatus.MotorChannels.Channel1);
                Console.WriteLine("Device Moved to {0}", newPos);

                // Tidy up and exit
                device.StopPolling();
                device.Disconnect(true);



                return new List<string> { "Ok" };
            });

            App.MapGet("/home/devices", async () =>
            {
                // Como procedimiento normal se trata de inicializar en la api la lista de los dispositivos conectados 
                try
                {
                    // Tell the device manager to get the list of all devices connected to the computer
                    DeviceManagerCLI.BuildDeviceList();
                }
                catch (Exception ex)
                {
                    // An error occurred - see ex for details
                    Console.WriteLine("Exception raised by BuildDeviceList {0}", ex);
                    return new List<string> { "No se ha podido iniciar la lista de dispositivos" };

                }
                List<string> serialNumbers = DeviceManagerCLI.GetDeviceList(KCubeInertialMotor.DevicePrefix_KIM101);
                return serialNumbers;
            });
        } 
    }
}

public class ObjMovement
{
   public string deviceId { get; set; }
   public int chaneltomove { get; set; }
   public int moveto { get; set; }
   public int steprate { get; set; }
   public int stepaceleration { get; set;  }
}