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
using Microsoft.AspNetCore.Builder;

namespace GotsThorlabs.NodesApi
{
    public class NodeHomepage
    {
        Decimal positionchanel = 0;
        // FrameSource frameSource;
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
                if (!builddeviceslist()) throw new InvalidOperationException("Error en coneccion a dispositivo Kim");
                List<string> serialNumbers = DeviceManagerCLI.GetDeviceList(KCubeInertialMotor.DevicePrefix_KIM101);

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
                    throw new InvalidOperationException("Error al abrir dispositivo Kim " + movestosite.deviceId);
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

                positionchanel = device.GetPosition(chanelsDevice[movestosite.chaneltomove]);//100
                int position = movestosite.moveto;//400
                //changet to use .moveto like a step movementerate no like a target point
                //position = ((int)positionchanel + position) == position ? (int)positionchanel + position : 100;
                position = (int)positionchanel + position;


                bool estatusMovement = Move_Method1(device, chanelsDevice[movestosite.chaneltomove], position);
                if (!estatusMovement)
                {
                    device.StopPolling();
                    device.Disconnect(true);
                    //return new List<string> { "Ocurrio un erro al mover el dispositvo" };
                    throw new InvalidOperationException("Error en coneccion a dispositivo Kim");
                }
                // or
                // Move_Method2(device, InertialMotorStatus.MotorChannels.Channel1, position);

                Decimal newPos = device.GetPosition(InertialMotorStatus.MotorChannels.Channel1);
                Console.WriteLine("Device Moved to {0}", newPos);
                ///////////// 1 uan funcion aparte 
                var responseKimStatus = new KimFourChanelsThorlabs();
                responseKimStatus.deviceId = movestosite.deviceId;

                foreach (var deviceinfo in chanelsDevice)
                {
                    var testdeviceinfo = device.GetJogParameters(chanelsDevice[deviceinfo.Key]);//lasa key inician en 1
                    var testdeviceinfo2 = device.GetPosition(chanelsDevice[deviceinfo.Key]);

                    var singlechannel = new KimSingleChanel();
                    singlechannel.rate = testdeviceinfo.JogRate;
                    singlechannel.aceleration = testdeviceinfo.JogAcceleration;
                    singlechannel.position = testdeviceinfo2;

                    responseKimStatus.UpdateKimArray(singlechannel, deviceinfo.Key - 1);

                }
                //////////////////// 1
                // Tidy up and exit
                device.StopPolling();
                device.Disconnect(true);

                //return new List<string> { "Ok" };////falta definir la respuesta
                //if(responseKimStatus != null)
                //{
                //return new ActionResult<KimFourChanelsThorlabs>(responseKimStatus)
                return Results.Ok(responseKimStatus);
                //}
                //return new List<string> { "Ok" };

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
            });//.RequireAuthorization();


            App.MapGet("Home/cameras", async () =>
            {
                //Dictionary<int, string> cameraslist = new Dictionary<int, string>();
                List<ObjCameras> cameralists = new List<ObjCameras>();
                int maxCameraIndex = 10; // Puedes ajustar esto según tus necesidades
                for (int i = 0; i < maxCameraIndex; i++)
                {
                    using (VideoCapture capture2 = new VideoCapture(i))
                    {
                        // Intentar abrir el dispositivo de captura
                        if (capture2.IsOpened())
                        {
                            string cameraName = capture2.GetBackendName().ToString(); //GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FriendlyName).ToString();
                            Console.WriteLine($"Cámara {i}: {cameraName} {capture2.CvPtr}");
                            //cameraslist.Add(i,cameraName);
                            cameralists.Add(new ObjCameras { cameraId = (int)i, cameraName = cameraName.ToString() });
                        }

                    }
                }
                return cameralists;
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

public class ObjCameras
{
    public int cameraId { get; set; }
    public string cameraName { get; set; }
}

public class KimFourChanelsThorlabs
{ 
    public string deviceId { get; set; } 

    public KimSingleChanel[] channelsInfo { get; set; }

    public KimFourChanelsThorlabs() { 
    
        channelsInfo = new KimSingleChanel[4];
    }

    public void UpdateKimArray(KimSingleChanel newitem,int channel)
    {
        channelsInfo[channel] = newitem;
    }
}

public class KimSingleChanel 
{ 
    public int position { get; set; }
    public int rate { get; set; }
    public int aceleration { get; set; }
    //public int size { get; set; }
}