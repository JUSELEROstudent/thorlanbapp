using GotsThorlabs.Interfaces;
using static OpenCvSharp.FileStorage;
using OpenCvSharp;

namespace GotsThorlabs.BLL
{
    public class ProcessTourData : IMakeAnStitching
    {
        public string ProcessTourDatawWhitStitchingPanorama(string PathName)
        {
            var carpetaPath = Path.Combine(Environment.CurrentDirectory, "StaticFiles", "datasetstitched");
            string[] archivos = Directory.GetFiles(carpetaPath, "*.jpg");
            Mat[] arraisMat = new Mat[archivos.Length];
            var output = new Mat();
            var outputarray1 = new Mat();
            int indexinter = 0;

            foreach (var archivo in archivos)
            {
                var img = new Mat(archivo.ToString());
                arraisMat[indexinter] = img;
                indexinter++;
            }

            var mode =   OpenCvSharp.Stitcher.Mode.Panorama;
            var stitched = Stitcher.Create(mode);
            //var nuevo1 = new BestOf2NearestMatcher();

            var solucion = stitched.Stitch(arraisMat, output);
            var estado = solucion == Stitcher.Status.OK ? true : false;
            var direccionsave = Path.Combine(Environment.CurrentDirectory, "StaticFiles", "openNative.jpg");
            output.SaveImage(direccionsave);
            return direccionsave;
            throw new NotImplementedException();
        }

        public string ProcessTourDatawWhitStitchingScans(string PathName)
        {
            var developerurl2 = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");

            var rand = new Random();
            var urlslocals = developerurl2.Split(";");
            var carpetaPath = Path.Combine(Environment.CurrentDirectory, "StaticFiles", PathName);
            string[] archivos = Directory.GetFiles(carpetaPath, "unitofpics*.jpg");
            Mat[] arraisMat = new Mat[archivos.Length];
            var output = new Mat();
            var outputarray1 = new Mat();
            int indexinter = 0;

            foreach (var archivo in archivos)
            {
                var img = new Mat(archivo.ToString());
                arraisMat[indexinter] = img;
                indexinter++;
            }

            var mode = OpenCvSharp.Stitcher.Mode.Scans ;
            var stitched = Stitcher.Create(mode);
            //var nuevo1 = new BestOf2NearestMatcher();

            var solucion = stitched.Stitch(arraisMat, output);
            var estado = solucion == Stitcher.Status.OK ? true : false;
            var direccionsave = Path.Combine(Environment.CurrentDirectory, "StaticFiles", PathName, "openNative.jpg");
            var urlstaticfiles1 = urlslocals[2] + $"/SouerceStaticFiles/{PathName}/" + "openNative.jpg" + "?ranmd=" + rand.Next().ToString();
            output.SaveImage(direccionsave);
            return urlstaticfiles1;
        }
    }
}
