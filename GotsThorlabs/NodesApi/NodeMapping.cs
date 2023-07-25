﻿using apitest.Controllers;
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
                //var listado = inertialmotorkim.deviceslist();
                // if (listado == null) { return Results.BadRequest("Verifique el estado de la conexion a los dispositivos THORLABS"); }
                //var deviceconnect = inertialmotorkim.Getobjdevicekim(listado[0]);
                // FUNCION CONECCION Y MOVER AHORA IMPLEMENTAR LA TOMA DE IMAGENES EN LA CLASE PARA CONTINUAR

                return Results.Ok(status);
            });
            App.MapGet("/automotion/channelsstatus", (string channel) =>
            {
                var inertialmotorkim = new Tim101_4_ch_inertial_motor();
                var status = inertialmotorkim.GetStatusChannels(channel);
                // METODO FUNCIONANDO = valorar la opcion de hacerlo por signalr tener en cuenta el polling que se hace al dispositivo . que es un llamado recurrente

                return Results.Ok(status);
            });

            App.MapGet("/automotion/stitcher", () =>
            {
                var inertialmotorkim = new Tim101_4_ch_inertial_motor();
                var status = inertialmotorkim.CreatesticherOpencv();

                return Results.Ok(status);
            });
        }
    }
}
