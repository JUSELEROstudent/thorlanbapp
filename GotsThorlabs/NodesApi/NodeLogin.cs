using apitest.Controllers;
namespace GotsThorlabs.NodesApi
{
    public class NodeLogin
    {
        public NodeLogin(WebApplication App)
        {

            // Getters de las peticiones.
            App.MapPost("/api/login", (login sesionuser) =>
            {
                var loginverificationclass = new Innerlogin();
                return loginverificationclass.Postdata(sesionuser);

            }
            );
        }
    }
}
