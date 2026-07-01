using AForge.Video.DirectShow;
using OpenCvSharp;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;

namespace GotsThorlabs.Services
{
    public class VideoCaptureCameraService : ICameraService, ICameraDiscoveryService, IDisposable
    {
        private readonly Dictionary<int, SemaphoreSlim> _deviceLocks = new();
        private readonly object _dictLock = new();
        private bool _disposed = false;

        public Mat CaptureFrame(int cameraId)
        {
            var sem = GetOrCreateLock(cameraId);
            sem.Wait();
            try
            {
                using var capture = new VideoCapture(cameraId, VideoCaptureAPIs.DSHOW);
                var frame = new Mat();
                if (!capture.IsOpened())
                {
                    capture.FrameWidth = 1920;
                    capture.FrameHeight = 1080;
                    capture.AutoFocus = true;
                }
                capture.Read(frame);
                return frame;
            }
            finally
            {
                sem.Release();
            }
        }

        public Mat CaptureFrame(string localIdentifier)
        {
            if (!int.TryParse(localIdentifier, out var cameraId))
                return new Mat();

            return CaptureFrame(cameraId);
            //return CaptureFrame(3);
        }

        // DirectShow opens/closes per capture, so there is no persistent
        // connection to release. No-op to satisfy the interface contract.
        public void ReleaseConnection() { }

        public IEnumerable<DeviceInfo> EnumerateDevices()
        {
            var result = new List<DeviceInfo>();
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            for (int i = 0; i < videoDevices.Count; i++)
            {
                var d = videoDevices[i];
                result.Add(new DeviceInfo
                {
                    Index = i,
                    // DirectShow devices have no serial number; use the index as stable identifier.
                    SerialNumber = i.ToString(),
                    DisplayName = d.Name ?? string.Empty,
                    MonikerString = d.MonikerString ?? string.Empty
                });
            }
            return result;
        }

        private SemaphoreSlim GetOrCreateLock(int cameraId)
        {
            lock (_dictLock)
            {
                if (!_deviceLocks.TryGetValue(cameraId, out var sem))
                {
                    sem = new SemaphoreSlim(1, 1);
                    _deviceLocks[cameraId] = sem;
                }
                return sem;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var sem in _deviceLocks.Values)
                sem.Dispose();
        }
    }
}
