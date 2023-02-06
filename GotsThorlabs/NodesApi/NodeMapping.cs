using apitest.Controllers;
using GotsThorlabs.BLL;

namespace GotsThorlabs.NodesApi
{
    public class NodeMapping
    {
        public NodeMapping (WebApplication App) {

            App.MapGet("/automotion/calibrate", async () =>
            {
                return Results.Ok("se logor comunicar");

            });
            App.MapGet("/automotion/mapping", () =>
            {
                var inertialmotorkim = new Tim101_4_ch_inertial_motor();
                var responser = inertialmotorkim.builddeviceslist();
                var listado = inertialmotorkim.deviceslist();
                Thread.Sleep(10000);

                return Results.Ok(responser != true ? responser : listado);
            });
        }
    }
}
