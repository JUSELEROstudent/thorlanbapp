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
            // inhabilitada por no ser de beta-test
            //App.MapGet("/automotion/mapping", async () =>
            //{
            //    var inertialmotorkim = new Tim101_4_ch_inertial_motor();
            //    bool status = inertialmotorkim.Createimagemosaic();
            //    //FUNCON QUE NO FUNCIONA MUCHO QUE SE DIGA DESPLIEGA LAS IMAGENES A UNA CARPETA CON DIRECCION FIJA

            //    return Results.Ok(status);
            //});
            //App.MapGet("/automotion/channelsstatus", (string channel) =>/// XXXXXno se tiene en cuenta el nombre de dispositivo kim     MEJOR CREAR UAN CLASE AUXILIAR PARA CONSULTAR LAS  COSAS
            //{
            //    var inertialmotorkim = new Tim101_4_ch_inertial_motor();
            //    var status = inertialmotorkim.GetStatusChannels(channel);
            //    // METODO FUNCIONANDO = valorar la opcion de hacerlo por signalr tener en cuenta el polling que se hace al dispositivo . que es un llamado recurrente

            //    return Results.Ok(status);
            //});

            //App.MapGet("/automotion/stitcher", (int mode) =>
            //{
            //    // 1. LLamar a la clase 2. traer los datos del recorrido y pasarlos al constructor // SECCION TAMBIEN DE beta-tester
            //    var inertialmotorkim = new Tim101_4_ch_inertial_motor();
            //    var status = inertialmotorkim.CreatesticherOpencv(mode);

            //    return Results.Ok(status);
            //});
        }
    }
}
