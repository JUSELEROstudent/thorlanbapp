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
using System;
namespace GotsThorlabs.BLL
{
    public class emgu
    {
        ///<summary>
        ///Funcion de stitch encargada de procesar la imagenes definidas. usando la libreria emgucv que es externa 
        ///</summary>
        ///<return>
        ///bool return, on OK return true 
        ///</return>

        public static bool getstitcher(int modes) 
        {
            var folderDatasetStitched = Path.Combine(Environment.CurrentDirectory, "StaticFiles", "datasetstitched");
            string[] filesIntoFolderDatasetStitched = Directory.GetFiles(folderDatasetStitched, "*.jpg");
            Mat[] arraisMat = new Mat[filesIntoFolderDatasetStitched.Length];

            var output = new Mat();
            int indexinter = 0;
            foreach (var fileImgStitched in filesIntoFolderDatasetStitched)
            {
                var img = new Mat(fileImgStitched.ToString());
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
            output.Save(Path.Combine(Environment.CurrentDirectory, "StaticFiles", "stitchemgu.jpg"));

            return resultado;
        }
    }
}
