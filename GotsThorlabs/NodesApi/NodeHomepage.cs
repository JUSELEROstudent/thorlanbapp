using Thorlabs.MotionControl.DeviceManagerCLI;
using Thorlabs.MotionControl.KCube.InertialMotorCLI;
using AForge.Video.DirectShow;
using AForge.Video;
using System.Drawing;
using OpenCvSharp;
using System.Diagnostics;

namespace GotsThorlabs.NodesApi
{
    public class NodeHomepage
    {
        FrameSource frameSource;
        public NodeHomepage(WebApplication App)
        {
            App.MapGet("/home/devices", async () =>
            {
                try
                {
                    // Tell the device manager to get the list of all devices connected to the computer
                    DeviceManagerCLI.BuildDeviceList();
                }
                catch (Exception ex)
                {
                    // An error occurred - see ex for details
                    Console.WriteLine("Exception raised by BuildDeviceList {0}", ex);
                    Console.ReadKey();

                }
                List<string> serialNumbers = DeviceManagerCLI.GetDeviceList(KCubeInertialMotor.DevicePrefix_KIM101);
                //CargaDispositivos();
                Readcameras();
                return serialNumbers;
            });
        }

        public void Readcameras() 
        {
            using var capture = new VideoCapture(0, VideoCaptureAPIs.DSHOW);
            if (!capture.IsOpened())
                return;

            capture.FrameWidth = 1920;
            capture.FrameHeight = 1280;
            capture.AutoFocus = true;

            const int sleepTime = 10;

            using var window = new Window("capture");
            var image = new Mat();

            capture.Read(image);
            string pathsave = string.Format("{0}\\camtaked.jpg", AppDomain.CurrentDomain.BaseDirectory);
            image.SaveImage(pathsave);

            while (true)
            {
                capture.Read(image);
                if (image.Empty())
                    break;

                window.ShowImage(image);
                int c = Cv2.WaitKey(sleepTime);
                if (c >= 0)
                {
                    break;
                }
            }

            //Mat cameraroll = new Mat();
            //frameSource.NextFrame(cameraroll);
            ////for (int i = 0; i < 10; i++) {

            ////    //var  resultado = camara.Read(cameraroll);
            ////    string imagePath = string.Format("{0}\\camtaked.jpg", AppDomain.CurrentDomain.BaseDirectory);
            ////    //Save the captured image
            ////    cameraroll.SaveImage(imagePath);
            ////}
        }

        //public void CargaDispositivos()
        //{
        //    MiDispositivos = new FilterInfoCollection(FilterCategory.VideoInputDevice);
        //    if (MiDispositivos.Count > 0)
        //    {
        //        HayDispositivos = true;
        //        for (int i = 0; i < MiDispositivos.Count; i++)
        //        System.Console.WriteLine (MiDispositivos[i].Name.ToString());
        //       // comboBox1.Text = MiDispositivos[0].ToString();
                
        //    }
        //    else
        //    {
        //        HayDispositivos = false;
        //    }
        //    activar_camara(MiDispositivos);
        //}
        //public void CerrarWebCam()
        //{
        //    if (MiWebCam != null && MiWebCam.IsRunning)
        //    {
        //        MiWebCam.SignalToStop();
        //        MiWebCam = null;
        //    }
        //}

        //private void activar_camara(FilterInfoCollection dispositivos)
        //{
        //    CerrarWebCam();
        //    //int i = comboBox1.SelectedIndex;
        //    //string NombreVideo = MiDispositivos[i].MonikerString;
        //    MiWebCam = new VideoCaptureDevice(dispositivos[0].Name.ToString());
        //    MiWebCam.NewFrame += new NewFrameEventHandler(Capturando);
        //    MiWebCam.Start();
        //}

        //private void Capturando(object sender, NewFrameEventArgs eventArgs)
        //{
        //    Bitmap Imagen = (Bitmap)eventArgs.Frame.Clone();
            
        //}

        //private void Form1_FormClosed(object sender)
        //{
        //    CerrarWebCam();
        //}

    }
}
