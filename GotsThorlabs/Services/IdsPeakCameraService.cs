using IDSImaging.Peak.API;
using IDSImaging.Peak.API.Core;
using IDSImaging.Peak.API.Core.Nodes;
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
                    Library.Initialize();
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

            Mat result = new Mat();
            try
            {
                using var buffer = dataStream.WaitForFinishedBuffer(3000);

                // Convert raw buffer to OpenCV Mat (BGR)
                int width = (int)buffer.Width();
                int height = (int)buffer.Height();

                // Read actual pixel format from device to choose the correct conversion.
                string pixelFormat = string.Empty;
                try
                {
                    pixelFormat = remoteNodemap.FindNode<EnumerationNode>("PixelFormat").CurrentEntry().SymbolicValue();
                    Console.WriteLine($"[IdsPeakCamera] PixelFormat={pixelFormat} width={width} height={height} payloadSize={payloadSize}");
                }
                catch (Exception ex) { Console.WriteLine($"[IdsPeakCamera] Could not read PixelFormat: {ex.Message}"); }

                // PayloadSize reflects the actual bytes from the camera.
                int rawSize = (int)payloadSize;
                byte[] rawData = new byte[rawSize];
                Marshal.Copy(buffer.BasePtr(), rawData, 0, rawData.Length);

                int totalPixels = width * height;
                int bytesPerPixel = totalPixels > 0 ? rawSize / totalPixels : 0;

                // Map GenICam/IDS PixelFormat names to OpenCV conversion codes.
                // IDS cameras can report names like "BayerRG8", "Bayer RG 8", "bayer_rg8", etc.
                // We normalize by removing spaces/underscores before matching.
                var bayerConversionMap = new Dictionary<string, ColorConversionCodes>(StringComparer.OrdinalIgnoreCase)
                {
                    { "BayerRG8",  ColorConversionCodes.BayerRG2BGR },
                    { "BayerBG8",  ColorConversionCodes.BayerBG2BGR },
                    { "BayerGR8",  ColorConversionCodes.BayerGR2BGR },
                    { "BayerGB8",  ColorConversionCodes.BayerGB2BGR },
                    // Alternative spellings seen in IDS SDK versions
                    { "bayer_rg8", ColorConversionCodes.BayerRG2BGR },
                    { "bayer_bg8", ColorConversionCodes.BayerBG2BGR },
                    { "bayer_gr8", ColorConversionCodes.BayerGR2BGR },
                    { "bayer_gb8", ColorConversionCodes.BayerGB2BGR },
                };

                // Normalize: remove spaces and underscores for flexible matching
                string pixelFormatNorm = pixelFormat.Replace(" ", "").Replace("_", "");

                if (!string.IsNullOrEmpty(pixelFormat) &&
                    (pixelFormatNorm.Equals("BGR8", StringComparison.OrdinalIgnoreCase) ||
                     pixelFormatNorm.Equals("RGB8", StringComparison.OrdinalIgnoreCase)))
                {
                    // 3-channel packed: copy raw bytes then fix channel order if RGB.
                    result = new Mat(height, width, MatType.CV_8UC3);
                    Marshal.Copy(rawData, 0, result.Data, rawData.Length);
                    if (pixelFormatNorm.Equals("RGB8", StringComparison.OrdinalIgnoreCase))
                        Cv2.CvtColor(result, result, ColorConversionCodes.RGB2BGR);
                }
                else if (!string.IsNullOrEmpty(pixelFormat) &&
                         bayerConversionMap.TryGetValue(pixelFormatNorm, out var bayerCode))
                {
                    // Bayer raw: demosaic using the correct pattern reported by the device.
                    using var rawMat = new Mat(height, width, MatType.CV_8UC1);
                    Marshal.Copy(rawData, 0, rawMat.Data, rawData.Length);
                    result = new Mat(height, width, MatType.CV_8UC3);
                    Cv2.CvtColor(rawMat, result, bayerCode);
                }
                else if (!string.IsNullOrEmpty(pixelFormat) &&
                         pixelFormatNorm.StartsWith("Mono", StringComparison.OrdinalIgnoreCase))
                {
                    // Monochrome: expand to 3-channel for consistency.
                    using var rawMat = new Mat(height, width, MatType.CV_8UC1);
                    Marshal.Copy(rawData, 0, rawMat.Data, rawData.Length);
                    result = new Mat(height, width, MatType.CV_8UC3);
                    Cv2.CvtColor(rawMat, result, ColorConversionCodes.GRAY2BGR);
                }
                else if (bytesPerPixel == 3)
                {
                    // Heuristic fallback: 3 bytes/pixel — assume BGR8 packed.
                    result = new Mat(height, width, MatType.CV_8UC3);
                    Marshal.Copy(rawData, 0, result.Data, rawData.Length);
                }
                else if (bytesPerPixel == 1)
                {
                    // Heuristic fallback: 1 byte/pixel — assume BayerRG8 (most common IDS sensor pattern).
                    using var rawMat = new Mat(height, width, MatType.CV_8UC1);
                    Marshal.Copy(rawData, 0, rawMat.Data, rawData.Length);
                    result = new Mat(height, width, MatType.CV_8UC3);
                    Cv2.CvtColor(rawMat, result, ColorConversionCodes.BayerRG2BGR);
                }
                else
                {
                    result = new Mat();
                }

                dataStream.QueueBuffer(buffer);
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

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            lock (_initLock)
            {
                if (_libraryInitialized)
                {
                    Library.Close();
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
