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
               // Readcameras();
                return serialNumbers;
            });
        }

        //public void Readcameras() 
        //{
        //    using var capture = new VideoCapture(0, VideoCaptureAPIs.DSHOW);
        //    if (!capture.IsOpened())
        //        return;

        //    capture.FrameWidth = 1920;
        //    capture.FrameHeight = 1280;
        //    capture.AutoFocus = true;

        //    const int sleepTime = 10;

        //    using var window = new Window("capture");
        //    var image = new Mat();

        //    capture.Read(image);
        //    string pathsave = string.Format("{0}\\camtaked.jpg", AppDomain.CurrentDomain.BaseDirectory);
        //    image.SaveImage(pathsave);

        //    while (true)
        //    {
        //        capture.Read(image);
        //        if (image.Empty())
        //            break;

        //        window.ShowImage(image);
        //        int c = Cv2.WaitKey(sleepTime);
        //        if (c >= 0)
        //        {
        //            break;
        //        }
        //    }
        //}

    }
}
