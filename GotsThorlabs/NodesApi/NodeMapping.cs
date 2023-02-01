using apitest.Controllers;

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
                Thread.Sleep(10000);

                return Results.Ok("Inicia mapeado");
            });
        }
    }
}
