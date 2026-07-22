using GotsThorlabs.BLL;
using GotsThorlabs.Models;
using Thorlabs.MotionControl.KCube.InertialMotorCLI;
using GotsThorlabs.Database.EntityRepo;
using Microsoft.EntityFrameworkCore;

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
            // NOTA: antes este endpoint usaba Tour.GetToursById (GotsThorlabs.Models.Tour),
            // que consulta con Dapper/ADO directo un archivo SQLite distinto
            // (Database/database/ThorlabsSql.db vía ConnectionSqlite) al que realmente usa
            // el resto de la app (database/app.sqlite vía ThorlabsDbContext/EF Core, que es
            // donde TakeTour.CreateTour() guarda los tours reales). Por eso nunca encontraba
            // los tours que se toman hoy: ese archivo viejo no existe o está vacío/desactualizado,
            // y GetToursById(...).First() lanzaba una excepción no controlada (500 genérico
            // sin explicación). Se corrige para leer del mismo ThorlabsDbContext que usa toda
            // la app, y se agregan validaciones (tour inexistente, carpeta inexistente, sin
            // imágenes suficientes) en vez de dejar que revienten como excepciones crudas.
            routes.MapPost("/api/Stitching/{idTour:long}", async (long idTour, ThorlabsDbContext db) =>
            {
                var tour = await db.Tours.AsNoTracking().FirstOrDefaultAsync(t => t.IdTour == idTour);
                if (tour == null)
                    return Results.NotFound($"No se encontró el tour con id {idTour}.");

                var tourFolder = Path.Combine(Environment.CurrentDirectory, "StaticFiles", tour.NameFolder);
                if (!Directory.Exists(tourFolder))
                    return Results.NotFound($"No existe la carpeta de imágenes del tour '{tour.NameFolder}'.");

                var unitPics = Directory.GetFiles(tourFolder, "unitofpics*.jpg");
                if (unitPics.Length < 2)
                    return Results.BadRequest($"El tour '{tour.NameFolder}' tiene {unitPics.Length} imagen(es) individual(es); se necesitan al menos 2 para hacer stitching.");

                try
                {
                    var stitchingobject = new ProcessTourData();
                    var stitchingresultPath = stitchingobject.ProcessTourDatawWhitStitchingScans(tour.NameFolder);
                    var fileName = Path.GetFileName(stitchingresultPath);
                    var url = $"/SouerceStaticFiles/{tour.NameFolder}/{fileName}";

                    return Results.Ok(new
                    {
                        idTour = tour.IdTour,
                        nameFolder = tour.NameFolder,
                        imagesUsed = unitPics.Length,
                        url
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Error al ejecutar el stitching: {ex.Message}", statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("GetStitchings")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            //routes.MapGet("/api/Stitching/{id}", (int id) =>
            //{
            //    //return new Stitching { ID = id };
            //})
            //.WithName("GetStitchingById")
            //.Produces<Stitching>(StatusCodes.Status200OK);

            //routes.MapPut("/api/Stitching/{id}", (int id, Stitching input) =>
            //{
            //    return Results.NoContent();
            //})
            //.WithName("UpdateStitching")
            //.Produces(StatusCodes.Status204NoContent);

            //routes.MapPost("/api/Stitching/", (Stitching model) =>
            //{
            //    //return Results.Created($"//api/Stitchings/{model.ID}", model);
            //})
            //.WithName("CreateStitching")
            //.Produces<Stitching>(StatusCodes.Status201Created);

            //routes.MapDelete("/api/Stitching/{id}", (int id) =>
            //{
            //    //return Results.Ok(new Stitching { ID = id });
            //})
            //.WithName("DeleteStitching")
            //.Produces<Stitching>(StatusCodes.Status200OK);
        }
    }
}
namespace GotsThorlabs.NodesApi
{
    public static class TourEndpoints
    {

        public static void MapTourEndpoints(this IEndpointRouteBuilder routes)
        {
            //routes.MapGet("/api/Tour", () =>
            //{
            //    return Tour.GetTours();
            //})
            //.WithName("GetAllTour")
            //.Produces<Stitching[]>(StatusCodes.Status200OK);

            //routes.MapGet("/api/Tour/{id}", (int id) =>
            //{
            //    //return new Stitching { ID = id };
            //})
            //.WithName("GetTourById")
            //.Produces<Stitching>(StatusCodes.Status200OK);

        }
    }
}