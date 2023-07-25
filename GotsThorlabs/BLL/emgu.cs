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
        //public emgu() { }

        public bool getstitcher() 
        {
            //var stitcher = OpenCvSharp.Stitcher.Mode.Scans;
            var output = new Mat();
            var img1 = new Mat("C:\\Users\\cocuy\\OneDrive\\Escritorio\\thorlabs\\GotsThorlabs\\GotsThorlabs\\StaticFiles\\stitchy1.png");
            //var img1 = new Mat("C:\\Users\\cocuy\\OneDrive\\Escritorio\\thorlabs\\GotsThorlabs\\GotsThorlabs\\StaticFiles\\stitche1.jpg", OpenCvSharp.ImreadModes.Color);
            var img2 = new Mat("C:\\Users\\cocuy\\OneDrive\\Escritorio\\thorlabs\\GotsThorlabs\\GotsThorlabs\\StaticFiles\\stitchy2.png");
            var img3 = new Mat("C:\\Users\\cocuy\\OneDrive\\Escritorio\\thorlabs\\GotsThorlabs\\GotsThorlabs\\StaticFiles\\stitchy3.png");
            //var img4 = new Mat("C:\\Users\\cocuy\\OneDrive\\Escritorio\\thorlabs\\GotsThorlabs\\GotsThorlabs\\StaticFiles\\stitche4.jpg" );


            
            Mat[] restulstitch3 = { img1, img2,img3 };
            VectorOfMat restulstitch = new VectorOfMat(restulstitch3);
            //var resultstitcher = OpenCvSharp.Stitcher.Create(stitcher);
            Brisk detector = new Brisk();

            Stitcher stitcher = new Stitcher();
            WarperCreator warper = new PlaneWarper();
            stitcher.SetFeaturesFinder(detector);
            stitcher.SetWarper(warper);

            var status = stitcher.Stitch(restulstitch, output);
            //var test1234 = InputArray.Create(restulstitch);
            //var resultadostitched = resultstitcher.Stitch(inputarrayINPUT, consolidado);

            var resultado = status == Stitcher.Status.Ok ? true : false;
            //var resultado2 = status == Stitcher.Status.Ok ? output.Save("C:\\Users\\cocuy\\OneDrive\\Escritorio\\thorlabs\\GotsThorlabs\\GotsThorlabs\\StaticFiles\\stitch.jpg") : "";
            output.Save("C:\\Users\\cocuy\\OneDrive\\Escritorio\\thorlabs\\GotsThorlabs\\GotsThorlabs\\StaticFiles\\stitch.jpg");
            //InputArray vectormat = InputArray.Create(restulstitch);
            //var respuesta = vectormat.IsMatVector();
            //var resultadostitched = resultstitcher.Stitch(vectormat, consolidado);


            //consolidado.SaveImage("C:\\Users\\cocuy\\OneDrive\\Escritorio\\thorlabs\\GotsThorlabs\\GotsThorlabs\\StaticFiles\\stitch.jpg");



            img1.Dispose();
            img2.Dispose();
            img3.Dispose();
            //img4.Dispose();
            return resultado;
        }
    }
}
