using OpenCvSharp;

namespace GotsThorlabs.Interfaces
{
    public interface ICameraService
    {
        /// <summary>
        /// Capture a single frame from a numeric camera index.
        /// </summary>
        /// <param name="cameraId">camera index</param>
        /// <returns>Captured frame as Mat (may be empty if capture failed)</returns>
        Mat CaptureFrame(int cameraId);

        /// <summary>
        /// Capture a single frame from the specified camera.
        /// Caller is responsible for disposing the returned Mat when done.
        /// </summary>
        /// <param name="localIdentifier">Device local identifier (serial number, display name, or numeric index string).</param>
        /// <returns>Captured frame as Mat (may be empty if capture failed)</returns>
        Mat CaptureFrame(string localIdentifier);
    }
}
