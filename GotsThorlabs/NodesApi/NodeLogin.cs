using apitest.Controllers;
using Thorlabs.MotionControl.DeviceManagerCLI;
using Thorlabs.MotionControl.KCube.InertialMotorCLI;
namespace GotsThorlabs.NodesApi
 
{
    public class NodeLogin
    {
        public NodeLogin(WebApplication App)
        {

            // Getters de las peticiones.
            App.MapPost("/api/login", (login sesionuser) =>
            {
                SimulationManager.Instance.InitializeSimulations();

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

                var loginverificationclass = new Innerlogin();
                return loginverificationclass.Postdata(sesionuser);

            }
            );
        }
    }
}
