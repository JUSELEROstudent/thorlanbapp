using apitest.Controllers;
using GotsThorlabs.BLL;
using Thorlabs.MotionControl.KCube.InertialMotorCLI;
using GotsThorlabs.Models;

namespace GotsThorlabs.NodesApi
{
    public class NodeMapping
    {
        public NodeMapping(WebApplication App)
        {

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
namespace GotsThorlabs.Stitchingapi
{
    public static class StitchingEndpoints
    {
        public static void MapStitchingEndpoints(this IEndpointRouteBuilder routes)
        {
            routes.MapGet("/api/Stitching", () =>
            {
                return new[] { new Stitching() };
            })
            .WithName("GetAllStitchings")
            .Produces<Stitching[]>(StatusCodes.Status200OK);

            routes.MapGet("/api/Stitching/{id}", (int id) =>
            {
                //return new Stitching { ID = id };
            })
            .WithName("GetStitchingById")
            .Produces<Stitching>(StatusCodes.Status200OK);

            routes.MapPut("/api/Stitching/{id}", (int id, Stitching input) =>
            {
                return Results.NoContent();
            })
            .WithName("UpdateStitching")
            .Produces(StatusCodes.Status204NoContent);

            routes.MapPost("/api/Stitching/", (Stitching model) =>
            {
                //return Results.Created($"//api/Stitchings/{model.ID}", model);
            })
            .WithName("CreateStitching")
            .Produces<Stitching>(StatusCodes.Status201Created);

            routes.MapDelete("/api/Stitching/{id}", (int id) =>
            {
                //return Results.Ok(new Stitching { ID = id });
            })
            .WithName("DeleteStitching")
            .Produces<Stitching>(StatusCodes.Status200OK);
        }
    }
}
namespace GotsThorlabs.NodesApi
{
    public static class TourEndpoints
    {
        public static void MapTourEndpoints(this IEndpointRouteBuilder routes)
        {
            routes.MapGet("/api/Tour", () =>
            {
                return Tour.GetTours();
            })
            .WithName("GetAllTour")
            .Produces<Stitching[]>(StatusCodes.Status200OK);

            routes.MapGet("/api/Tour/{id}", (int id) =>
            {
                //return new Stitching { ID = id };
            })
            .WithName("GetTourById")
            .Produces<Stitching>(StatusCodes.Status200OK);

        }
    }
}