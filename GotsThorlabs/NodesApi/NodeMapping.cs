using apitest.Controllers;
using GotsThorlabs.BLL;
using Thorlabs.MotionControl.KCube.InertialMotorCLI;

namespace GotsThorlabs.NodesApi
{
    public class NodeMapping
    {
        public NodeMapping (WebApplication App) {

            App.MapGet("/automotion/calibrate", async () =>
            {
                return Results.Ok("se logor comunicar");

            });
            App.MapGet("/automotion/mapping", async () =>
            {
                var inertialmotorkim = new Tim101_4_ch_inertial_motor();
                bool status = inertialmotorkim.Createimagemosaic();
                //FUNCON QUE NO FUNCIONA MUCHO QUE SE DIGA DESPLIEGA LAS IMAGENES A UNA CARPETA CON DIRECCION FIJA

                return Results.Ok(status);
            });
            App.MapGet("/automotion/channelsstatus", (string channel) =>
            {
                var inertialmotorkim = new Tim101_4_ch_inertial_motor();
                var status = inertialmotorkim.GetStatusChannels(channel);
                // METODO FUNCIONANDO = valorar la opcion de hacerlo por signalr tener en cuenta el polling que se hace al dispositivo . que es un llamado recurrente

                return Results.Ok(status);
            });

            App.MapGet("/automotion/stitcher", (int mode) =>
            {
                var inertialmotorkim = new Tim101_4_ch_inertial_motor();
                var status = inertialmotorkim.CreatesticherOpencv(mode);

                return Results.Ok(status);
            });
        }
    }
}
