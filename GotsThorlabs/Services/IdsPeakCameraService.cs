using IDSImaging.Peak.API;
using IDSImaging.Peak.API.Core;
using IDSImaging.Peak.API.Core.Nodes;
using IDSImaging.Peak.IPL;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using OpenCvSharp;
using System.Runtime.InteropServices;

namespace GotsThorlabs.Services
{
    /// <summary>
    /// Camera service for IDS scientific cameras using the IDSImaging.Peak.API (ids_peak_dotnet) SDK.
    /// Singleton: only one acquisition session is opened at a time, guarded by a lock per cameraId.
    /// </summary>
    public class IdsPeakCameraService : ICameraService, ICameraDiscoveryService, IDisposable
    {
        private static bool _libraryInitialized = false;
        private static readonly object _initLock = new();

        // Per-cameraId acquisition lock so concurrent requests block rather than collide.
        private readonly Dictionary<int, SemaphoreSlim> _deviceLocks = new();
        private readonly Dictionary<string, SemaphoreSlim> _deviceLocksByIdentifier = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _dictLock = new();
        private bool _disposed = false;

        public Mat CaptureFrame(int cameraId)
        {
            var sem = GetOrCreateLock(cameraId);
            sem.Wait();
            try
            {
                return CaptureInternal(cameraId);
            }
            finally
            {
                sem.Release();
            }
        }

        public IdsPeakCameraService()
        {
            lock (_initLock)
            {
                if (!_libraryInitialized)
                {
                    IDSImaging.Peak.API.Library.Initialize();
                    _libraryInitialized = true;
                }
            }
        }

        public Mat CaptureFrame(string localIdentifier)
        {
            var sem = GetOrCreateIdentifierLock(localIdentifier);
            sem.Wait();
            try
            {
                return CaptureInternal(localIdentifier);
            }
            finally
            {
                sem.Release();
            }
        }

        private Mat CaptureInternal(int cameraId)
        {
            var deviceManager = DeviceManager.Instance();
            deviceManager.Update(
                DeviceManager.UpdatePolicy.ScanEnvironmentForProducerLibraries,
                (string msg) => { /* ignore update errors */ });

            var devices = deviceManager.Devices();
            if (devices.Count == 0 || cameraId < 0 || cameraId >= devices.Count)
                return new Mat();

            using var device = devices[cameraId].OpenDevice(DeviceAccessType.Control);
            return CaptureFromOpenedDevice(device);
        }

        private Mat CaptureInternal(string localIdentifier)
        {
            var deviceManager = DeviceManager.Instance();
            deviceManager.Update(
                DeviceManager.UpdatePolicy.ScanEnvironmentForProducerLibraries,
                (string msg) => { /* ignore update errors */ });

            var devices = deviceManager.Devices();
            if (devices.Count == 0)
                return new Mat(); // empty frame: no device

            int resolvedIndex = -1;

            // Quick path: if caller passed a numeric index as string, use it directly.
            if (int.TryParse(localIdentifier, out var numericIndex))
            {
                if (numericIndex >= 0 && numericIndex < devices.Count)
                    resolvedIndex = numericIndex;
            }
            else
            {
                // Find device by matching serial number or display name against localIdentifier.
                // Accept exact matches or substring matches so moniker strings (DirectShow/AForge-style)
                // that contain the serial or display name will still match the IDS device list.
                for (int i = 0; i < devices.Count; i++)
                {
                    var d = devices[i];
                    var serial = d.SerialNumber() ?? string.Empty;
                    var display = d.DisplayName() ?? string.Empty;

                    if (string.Equals(serial, localIdentifier, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(display, localIdentifier, StringComparison.OrdinalIgnoreCase)
                        || (!string.IsNullOrEmpty(serial) && localIdentifier.IndexOf(serial, StringComparison.OrdinalIgnoreCase) >= 0)
                        || (!string.IsNullOrEmpty(display) && localIdentifier.IndexOf(display, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        resolvedIndex = i;
                        break;
                    }
                }
            }

            if (resolvedIndex < 0)
                return new Mat(); // no matching device found

            using var device = devices[resolvedIndex].OpenDevice(DeviceAccessType.Control);
            return CaptureFromOpenedDevice(device);
        }

        private Mat CaptureFromOpenedDevice(Device device)
        {
            using var remoteNodemap = device.RemoteDevice().NodeMaps()[0];

            using var dataStream = device.DataStreams()[0].OpenDataStream();

            var payloadSize = remoteNodemap.FindNode<IntegerNode>("PayloadSize").Value();
            var minBuffers = dataStream.NumBuffersAnnouncedMinRequired();

            for (var i = 0; i < minBuffers; ++i)
            {
                var buff = dataStream.AllocAndAnnounceBuffer((uint)payloadSize, IntPtr.Zero);
                dataStream.QueueBuffer(buff);
            }

            remoteNodemap.FindNode<IntegerNode>("TLParamsLocked").SetValue(1);
            dataStream.StartAcquisition();
            remoteNodemap.FindNode<CommandNode>("AcquisitionStart").Execute();
            remoteNodemap.FindNode<CommandNode>("AcquisitionStart").WaitUntilDone();

            // Discard warm-up frames so auto-exposure/auto-white-balance can settle
            // before the real capture. 3 frames is enough for most IDS sensors.
            const int warmUpFrames = 3;
            for (int w = 0; w < warmUpFrames; w++)
            {
                try
                {
                    using var warmBuf = dataStream.WaitForFinishedBuffer(2000);
                    dataStream.QueueBuffer(warmBuf);
                }
                catch { break; }
            }

            Mat result = new Mat();
            try
            {
                // Retry up to 3 times to get a complete (non-corrupted) buffer.
                IDSImaging.Peak.API.Core.Buffer? buffer = null;
                const int maxRetries = 3;
                for (int attempt = 0; attempt < maxRetries; attempt++)
                {
                    var candidate = dataStream.WaitForFinishedBuffer(3000);
                    if (!candidate.IsIncomplete())
                    {
                        buffer = candidate;
                        break;
                    }
                    Console.WriteLine($"[IdsPeakCamera] Incomplete buffer on attempt {attempt + 1}, retrying...");
                    dataStream.QueueBuffer(candidate);
                }

                if (buffer == null)
                {
                    Console.WriteLine("[IdsPeakCamera] Could not acquire a complete buffer after retries.");
                    return result;
                }

                using var _ = buffer;

                int width = (int)buffer.Width();
                int height = (int)buffer.Height();

                // Read pixel format reported by the buffer (reflects device active format).
                string pixelFormatName = string.Empty;
                try
                {
                    pixelFormatName = remoteNodemap.FindNode<EnumerationNode>("PixelFormat").CurrentEntry().SymbolicValue();
                    Console.WriteLine($"[IdsPeakCamera] PixelFormat={pixelFormatName} width={width} height={height} payloadSize={payloadSize}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[IdsPeakCamera] Could not read PixelFormat: {ex.Message}");
                }

                // Build an IPL Image directly from the raw buffer pointer — same path used by the vendor application.
                // IPL handles Bayer demosaicing and color correction internally, producing identical results to
                // the IDS cockpit / IDS peak viewer.
                var iplPixelFormat = new IDSImaging.Peak.IPL.PixelFormat(
                    Enum.TryParse<PixelFormatName>(pixelFormatName, out var pfn)
                        ? pfn
                        : PixelFormatName.BayerRG8);

                using var iplImage = new IDSImaging.Peak.IPL.Image(
                    iplPixelFormat,
                    buffer.BasePtr(),
                    (uint)payloadSize,
                    (uint)width,
                    (uint)height);

                // Convert to BGR8 (OpenCV native order) using IPL's built-in high-quality debayering.
                var bgrFormat = new IDSImaging.Peak.IPL.PixelFormat(PixelFormatName.BGR8);
                using var bgrImage = iplImage.ConvertTo(bgrFormat);

                // Copy IPL BGR8 result into an OpenCV Mat.
                int bgrSize = width * height * 3;
                result = new Mat(height, width, MatType.CV_8UC3);
                unsafe
                {
                    System.Buffer.MemoryCopy(bgrImage.Data().ToPointer(), result.Data.ToPointer(), bgrSize, bgrSize);
                }
            }
            finally
            {
                remoteNodemap.FindNode<CommandNode>("AcquisitionStop").Execute();
                remoteNodemap.FindNode<CommandNode>("AcquisitionStop").WaitUntilDone();
                dataStream.StopAcquisition(AcquisitionStopMode.Default);
                dataStream.Flush(DataStreamFlushMode.DiscardAll);
                foreach (var b in dataStream.AnnouncedBuffers())
                    dataStream.RevokeBuffer(b);
                remoteNodemap.FindNode<IntegerNode>("TLParamsLocked").SetValue(0);
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

        private SemaphoreSlim GetOrCreateIdentifierLock(string localIdentifier)
        {
            lock (_dictLock)
            {
                if (!_deviceLocksByIdentifier.TryGetValue(localIdentifier, out var sem))
                {
                    sem = new SemaphoreSlim(1, 1);
                    _deviceLocksByIdentifier[localIdentifier] = sem;
                }
                return sem;
            }
        }

        public IEnumerable<DeviceInfo> EnumerateDevices()
        {
            var deviceManager = DeviceManager.Instance();
            deviceManager.Update(
                DeviceManager.UpdatePolicy.ScanEnvironmentForProducerLibraries,
                (string msg) => { /* ignore update errors */ });

            var devices = deviceManager.Devices();
            var result = new List<DeviceInfo>();
            for (int i = 0; i < devices.Count; i++)
            {
                var d = devices[i];
                result.Add(new DeviceInfo
                {
                    Index = i,
                    SerialNumber = d.SerialNumber() ?? string.Empty,
                    DisplayName = d.DisplayName() ?? string.Empty,
                    MonikerString = string.Empty
                });
            }
            return result;
        }

        // Peak acquisition opens/closes per capture, so there is no persistent
        // connection to release. No-op to satisfy the interface contract.
        public void ReleaseConnection() { }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            lock (_initLock)
            {
                if (_libraryInitialized)
                {
                    IDSImaging.Peak.API.Library.Close();
                    _libraryInitialized = false;
                }
            }
            foreach (var sem in _deviceLocks.Values)
                sem.Dispose();
            foreach (var sem in _deviceLocksByIdentifier.Values)
                sem.Dispose();
        }
    }
}
