using OpenCvSharp;
using GotsThorlabs.Interfaces;

namespace GotsThorlabs.Services
{
    public class VideoCaptureCameraService : ICameraService
    {
        public Mat CaptureFrame(int cameraId)
        {
            using var capture = new VideoCapture(cameraId, VideoCaptureAPIs.DSHOW);
            var frame = new Mat();
            if (!capture.IsOpened())
            {
                // try to set defaults and reopen
                capture.FrameWidth = 1920;
                capture.FrameHeight = 1080;
                capture.AutoFocus = true;
            }
            capture.Read(frame);
            return frame; // caller will dispose
        }
    }
}
