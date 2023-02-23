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
            App.MapPost("/api/login", async (login sesionuser) =>
            {
                var loginverificationclass = new InnerloginController();
                return Results.Ok(loginverificationclass.Post(sesionuser));

            }
            );
        }
    }
}
