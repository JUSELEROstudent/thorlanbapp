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

        /// <summary>
        /// Releases any persistent camera connection so the device is free for
        /// use by other software (e.g. the vendor's own app). Safe to call even
        /// when no connection is open. Implementations that open/close per
        /// capture may leave this as a no-op.
        /// </summary>
        void ReleaseConnection();
    }
}
