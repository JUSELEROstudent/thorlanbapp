using Emgu.CV.Stitching;
using Emgu.CV;
//using Emgu.CV;
//using Emgu.CV.Flann;
//using Emgu.CV.CvEnum;
//using Emgu.CV.OCR;
//using Emgu.CV.ML.MlEnum;
//using Emgu.CV.ML;
//using Emgu.CV.Face;
using Emgu.CV.Util;
using Emgu.CV.Features2D;
using Emgu.Util;
namespace GotsThorlabs.BLL
{
    public class emgu
    {
        ///<summary>
        ///Funcion de stitch encargada de procesar la imagenes definidas. usando la libreria emgucv qeu es externa 
        ///</summary>
        ///<return>
        ///bool return, on OK return true 
        ///</return>

        public bool getstitcher(int modes) 
        {
            var currentpath = Directory.GetCurrentDirectory();
            var carpetaPath = currentpath + "\\StaticFiles\\datasetstitched";
            string[] archivos = Directory.GetFiles(carpetaPath, "*.jpg");
            Mat[] arraisMat = new Mat[archivos.Length];

            var output = new Mat();
            int indexinter = 0;
            foreach (var archivo in archivos)
            {
                var img = new Mat(archivo.ToString());
                arraisMat[indexinter] = img;
                indexinter++;
            }
            VectorOfMat restulstitch = new VectorOfMat(arraisMat);
            Brisk detector = new Brisk();

            Stitcher stitcher = new Stitcher();
            WarperCreator warper1 = new PlaneWarper();
            WarperCreator warper2 = new  Emgu.CV.Stitching.CylindricalWarper();
            var warper = modes == 0 ? warper1 : warper2;
            stitcher.SetFeaturesFinder(detector);
            stitcher.SetWarper(warper);

            var status = stitcher.Stitch(restulstitch, output);

            var resultado = status == Stitcher.Status.Ok ? true : false;
            output.Save(currentpath + "\\StaticFiles\\stitchemgu.jpg");

            return resultado;
        }
    }
}
