using OpenCvSharp;

namespace GotsThorlabs.Interfaces
{
    public interface ICameraService
    {
        /// <summary>
        /// Capture a single frame from the specified camera index.
        /// Caller is responsible for disposing the returned Mat when done.
        /// </summary>
        /// <param name="cameraId">camera index or identifier</param>
        /// <returns>Captured frame as Mat (may be empty if capture failed)</returns>
        Mat CaptureFrame(int cameraId);
    }
}
