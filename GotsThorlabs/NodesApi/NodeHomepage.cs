using Thorlabs.MotionControl.DeviceManagerCLI;
using Thorlabs.MotionControl.KCube.InertialMotorCLI;
namespace GotsThorlabs.NodesApi
{
    public class NodeHomepage
    {
        public NodeHomepage(WebApplication App)
        {
            App.MapGet("/home/devices", async () =>
            {
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
                return serialNumbers;
            });
        }
    }
}
