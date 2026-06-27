using uEye;
using uEye.Defines;
using uEye.Types;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using OpenCvSharp;
using System.Runtime.InteropServices;

namespace GotsThorlabs.Services
{
    /// <summary>
    /// Camera service for IDS cameras using the native uEye SDK (uEyeDotNet.dll).
    /// 
    /// ─────────────────────────────────────────────────────────────────
    ///  DRIVER SELECTOR — change to switch between capture implementations
    /// ─────────────────────────────────────────────────────────────────
    ///  "ids_peak_dotnet"  → IDSImaging.Peak.API NuGet (GenTL / USB3 Vision)
    ///  "ids_ueye"         → uEyeDotNet.dll  (classic uEye USB / GigE cameras)
    ///
    ///  To activate this driver set DriverType = "ids_ueye" on the camera
    ///  record in the database (or update the default in CameraServiceFactory).
    /// ─────────────────────────────────────────────────────────────────
    /// </summary>
    public class IdsUEyeCameraService : ICameraService, ICameraDiscoveryService, IDisposable
    {
        private bool _disposed;

        // ═══════════════════════════════════════════════════════════════
        //  PROFILE SELECTOR — change this value to switch capture profile
        // ═══════════════════════════════════════════════════════════════
        //
        //  UEyeProfile.Default        → camera.Parameter.ResetToDefault()
        //                               Resets all settings to factory defaults.
        //
        //  UEyeProfile.HardwareStored → camera.Parameter.Load()
        //                               Loads the parameter set stored inside
        //                               the camera hardware (saved via uEye Cockpit).
        //
        //  UEyeProfile.FromFile       → camera.Parameter.Load(ProfileFilePath)
        //                               Loads a .ini file exported from uEye Cockpit.
        //                               Set ProfileFilePath below.
        //
        //  UEyeProfile.SceneAutomatic → SceneMode.Automatic  (general auto)
        //  UEyeProfile.ScenePortrait  → SceneMode.Portrait
        //  UEyeProfile.SceneSunny     → SceneMode.Sunny
        //  UEyeProfile.SceneNight     → SceneMode.Night
        //  UEyeProfile.SceneSports    → SceneMode.Sports
        //  UEyeProfile.SceneLandscape → SceneMode.Landscape
        //
        private const UEyeProfile ActiveProfile = UEyeProfile.Default;

        // Path to .ini file — only used when ActiveProfile = UEyeProfile.FromFile
        private const string ProfileFilePath = @"C:\Users\cocuy\Pictures\Feedback\camera_profile.ini";
        // ═══════════════════════════════════════════════════════════════

        // Warm-up frames to let auto-exposure/AWB settle before the real capture.
        private const int WarmUpFrames = 3;

        // ── ICameraService ────────────────────────────────────────────

        public Mat CaptureFrame(int cameraId)
            => CaptureInternal(cameraId.ToString());

        public Mat CaptureFrame(string localIdentifier)
            => CaptureInternal(localIdentifier);

        // ── ICameraDiscoveryService ───────────────────────────────────

        public IEnumerable<DeviceInfo> EnumerateDevices()
        {
            CameraInformation[]? list;
            uEye.Info.Camera.GetCameraList(out list);

            var result = new List<DeviceInfo>();
            if (list == null) return result;

            for (int i = 0; i < list.Length; i++)
            {
                result.Add(new DeviceInfo
                {
                    Index        = i,
                    SerialNumber = list[i].SerialNumber ?? string.Empty,
                    DisplayName  = list[i].Model        ?? string.Empty,
                    MonikerString = string.Empty
                });
            }
            return result;
        }

        // ── Internal capture ─────────────────────────────────────────

        private Mat CaptureInternal(string localIdentifier)
        {
            CameraInformation[]? cameraList;
            uEye.Info.Camera.GetCameraList(out cameraList);

            if (cameraList == null || cameraList.Length == 0)
                return new Mat();

            int deviceId = ResolveDeviceId(cameraList, localIdentifier);
            if (deviceId < 0)
                return new Mat();

            var camera = new Camera();
            Status status = camera.Init(deviceId);
            if (status != Status.Success)
            {
                Console.WriteLine($"[IdsUEye] Init failed for deviceId={deviceId}: {status}");
                return new Mat();
            }

            try
            {
                // Apply the selected profile FIRST — ResetToDefault()/Load() can change
                // the pixel format, so any PixelFormat.Set() done before this would be
                // overwritten (causing raw-Bayer output → black/white, torn images).
                ApplyProfile(camera);

                // Force BGR8 output AFTER the profile so it is not reset to raw Bayer.
                // BGR8Packed makes CopyToArray deliver 3-channel packed bytes (color).
                camera.PixelFormat.Set(ColorMode.BGR8Packed);

                // Allocate the capture buffer AFTER the pixel format is locked, so it
                // is sized correctly for BGR8 (3 bytes/pixel).
                int memId;
                camera.Memory.Allocate(out memId, true);

                // Always ensure freerun mode (TriggerMode.Off) regardless of what the profile loaded.
                // In freerun the camera captures continuously and Freeze() takes the next ready frame.
                // If a trigger mode is active, Freeze() would block indefinitely waiting for an external signal.
                camera.Trigger.Set(TriggerMode.Off);
                Console.WriteLine("[IdsUEye] Trigger set to Off (freerun)");

                // Converge exposure (auto-shutter only, no auto-gain) and white balance.
                ApplyAutoExposure(camera);

                // Real capture.
                status = camera.Acquisition.Freeze(DeviceParameter.Wait);
                if (status != Status.Success)
                {
                    Console.WriteLine($"[IdsUEye] Freeze failed: {status}");
                    return new Mat();
                }

                // Always read from the LAST completed buffer, not the allocated memId,
                // because the SDK may have used a different slot internally.
                int lastMemId;
                camera.Memory.GetLast(out lastMemId);
                camera.Memory.Lock(lastMemId);

                int width = 0, height = 0, pitch = 0;
                camera.Memory.GetWidth(lastMemId, out width);
                camera.Memory.GetHeight(lastMemId, out height);
                camera.Memory.GetPitch(lastMemId, out pitch);

                if (width == 0 || height == 0)
                {
                    camera.Memory.Unlock(lastMemId);
                    return new Mat();
                }

                // Get a native pointer to the buffer and copy using stride (pitch).
                IntPtr bufPtr;
                camera.Memory.ToIntPtr(lastMemId, out bufPtr);

                Console.WriteLine($"[IdsUEye] Captured width={width} height={height} pitch={pitch}");

                var mat = new Mat(height, width, MatType.CV_8UC3);
                unsafe
                {
                    byte* src = (byte*)bufPtr.ToPointer();
                    byte* dst = (byte*)mat.Data.ToPointer();
                    int rowBytes = width * 3;
                    for (int row = 0; row < height; row++)
                        System.Buffer.MemoryCopy(src + row * pitch, dst + row * rowBytes, rowBytes, rowBytes);
                }

                camera.Memory.Unlock(lastMemId);

                // DEBUG: save both the raw uEye bytes and the resulting OpenCV Mat for visual comparison.
                // TODO: remove this block once color/capture issues are resolved.
                ////////////SaveDebugFrames(bufPtr, width, height, pitch, mat);//descomentar cuandose quiera ver la imagen en ambios casos cv y peak

                return mat;
            }
            finally
            {
                camera.Exit();
            }
        }

        // ── Helpers ──────────────────────────────────────────────────

        private static int ResolveDeviceId(CameraInformation[] list, string localIdentifier)
        {
            // 1. Numeric index fast path.
            if (int.TryParse(localIdentifier, out var idx) && idx >= 0 && idx < list.Length)
                return list[idx].DeviceID;

            // 2. Match by serial number or model name (exact or substring).
            for (int i = 0; i < list.Length; i++)
            {
                var serial = list[i].SerialNumber ?? string.Empty;
                var model  = list[i].Model        ?? string.Empty;

                if (string.Equals(serial, localIdentifier, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(model, localIdentifier,  StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrEmpty(serial) && localIdentifier.IndexOf(serial, StringComparison.OrdinalIgnoreCase) >= 0)
                    || (!string.IsNullOrEmpty(model)  && localIdentifier.IndexOf(model,  StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return list[i].DeviceID;
                }
            }

            return -1;
        }

        // ── Profile loading ──────────────────────────────────────────
        private static void ApplyProfile(Camera camera)
        {
            Console.WriteLine($"[IdsUEye] Applying profile: {ActiveProfile}");
            Status st;
            switch (ActiveProfile)
            {
                case UEyeProfile.Default:
                    st = camera.Parameter.ResetToDefault();
                    Console.WriteLine($"[IdsUEye] ResetToDefault: {st}");
                    break;

                case UEyeProfile.HardwareStored:
                    st = camera.Parameter.Load();
                    Console.WriteLine($"[IdsUEye] Parameter.Load (hardware): {st}");
                    break;

                case UEyeProfile.FromFile:
                    st = camera.Parameter.Load(ProfileFilePath);
                    Console.WriteLine($"[IdsUEye] Parameter.Load (file={ProfileFilePath}): {st}");
                    break;

                case UEyeProfile.SceneAutomatic:
                    st = camera.ScenePreset.Set(SceneMode.Automatic);
                    Console.WriteLine($"[IdsUEye] ScenePreset.Automatic: {st}");
                    break;

                case UEyeProfile.ScenePortrait:
                    st = camera.ScenePreset.Set(SceneMode.Portrait);
                    Console.WriteLine($"[IdsUEye] ScenePreset.Portrait: {st}");
                    break;

                case UEyeProfile.SceneSunny:
                    st = camera.ScenePreset.Set(SceneMode.Sunny);
                    Console.WriteLine($"[IdsUEye] ScenePreset.Sunny: {st}");
                    break;

                case UEyeProfile.SceneNight:
                    st = camera.ScenePreset.Set(SceneMode.Night);
                    Console.WriteLine($"[IdsUEye] ScenePreset.Night: {st}");
                    break;

                case UEyeProfile.SceneSports:
                    st = camera.ScenePreset.Set(SceneMode.Sports);
                    Console.WriteLine($"[IdsUEye] ScenePreset.Sports: {st}");
                    break;

                case UEyeProfile.SceneLandscape:
                    st = camera.ScenePreset.Set(SceneMode.Landscape);
                    Console.WriteLine($"[IdsUEye] ScenePreset.Landscape: {st}");
                    break;
            }
        }
        // ── END Profile loading ──────────────────────────────────────

        // ── Auto-exposure + white balance convergence ────────────────
        // Overexposure ("quemado") fix: auto-GAIN is the main culprit because it
        // amplifies the sensor signal and pushes brightness far above mid-gray,
        // adding noise and washing out colors. We therefore:
        //   1. Disable auto-gain entirely and zero the hardware master gain.
        //   2. Disable the gain boost (prevents analog amplification).
        //   3. Let ONLY auto-shutter (exposure time) converge brightness — this is
        //      the natural, noise-free way to control exposure.
        //   4. Enable auto white balance once (ActivateMode.Once) so the camera
        //      computes a faithful color balance and then auto-disables itself.
        private static void ApplyAutoExposure(Camera camera)
        {
            const double OverexposedThreshold = 170.0;   // lower than before: 170/255 is already too bright
            const int    MaxConvergeFrames   = 40;
            const int    MinConvergeFrames   = 8;        // give shutter time to settle

            // ── 1. Kill AGC (auto gain) so the image is not "quemada". ──
            bool gainSupported = false;
            camera.AutoFeatures.Sensor.Gain.GetSupported(out gainSupported);
            if (gainSupported) camera.AutoFeatures.Sensor.Gain.SetEnable(false);

            bool swGainSupported = false;
            camera.AutoFeatures.Software.Gain.GetSupported(out swGainSupported);
            if (swGainSupported) camera.AutoFeatures.Software.Gain.SetEnable(false);

            // Zero the hardware master gain and turn off gain boost (no analog amplification).
            try { camera.Gain.Hardware.Scaled.SetMaster(0); } catch (Exception ex) { Console.WriteLine($"[IdsUEye] SetMaster(0) failed: {ex.Message}"); }
            try { camera.Gain.Hardware.Boost.SetEnable(false); } catch (Exception ex) { Console.WriteLine($"[IdsUEye] Boost disable failed: {ex.Message}"); }

            // ── 2. Enable auto-white-balance ONCE for faithful colors. ──
            // RunOnce converges the WB gains then auto-disables, locking the color balance.
            bool swWhiteSupported = false;
            camera.AutoFeatures.Software.WhiteBalance.GetSupported(out swWhiteSupported);
            if (swWhiteSupported)
            {
                try { camera.AutoFeatures.Software.WhiteBalance.SetEnable(uEye.Defines.ActivateMode.Once); }
                catch (Exception ex) { Console.WriteLine($"[IdsUEye] AWB Once failed: {ex.Message}"); }
            }

bool sensorWhiteSupported = false;
            camera.AutoFeatures.Sensor.Whitebalance.GetSupported(out sensorWhiteSupported);
            if (sensorWhiteSupported)
            {
                // Sensor white balance uses WhiteBalanceMode (Enable/Disable), not ActivateMode.
                // The software AWB above (Once) handles the one-shot color convergence;
                // for the sensor path we simply enable it and disable it after convergence.
                try { camera.AutoFeatures.Sensor.Whitebalance.SetEnable(uEye.Defines.Whitebalance.WhiteBalanceMode.Automatic); } // se supone que era enable pero no lo tiene incluido 
                catch (Exception ex) { Console.WriteLine($"[IdsUEye] Sensor AWB Enable failed: {ex.Message}"); }
            }

            Console.WriteLine($"[IdsUEye] AutoExposure setup: gainAutoSensor={gainSupported} gainAutoSW={swGainSupported} awbSW={swWhiteSupported} awbSensor={sensorWhiteSupported}");

            // ── 3. Enable auto-shutter (exposure time) only — the natural exposure control. ──
            bool shutterSupported = false;
            camera.AutoFeatures.Sensor.Shutter.GetSupported(out shutterSupported);
            if (shutterSupported) camera.AutoFeatures.Sensor.Shutter.SetEnable(true);

            bool swShutterSupported = false;
            camera.AutoFeatures.Software.Shutter.GetSupported(out swShutterSupported);
            if (swShutterSupported) camera.AutoFeatures.Software.Shutter.SetEnable(true);

            Console.WriteLine($"[IdsUEye] AutoExposure: sensorShutter={shutterSupported} swShutter={swShutterSupported}");

            // ── 4. Feed frames until the shutter converges brightness below the threshold. ──
            double lastMean = double.MaxValue;
            for (int i = 0; i < MaxConvergeFrames; i++)
            {
                var st = camera.Acquisition.Freeze(DeviceParameter.Wait);
                if (st != Status.Success) break;

                int lastId;
                camera.Memory.GetLast(out lastId);
                if (lastId <= 0) continue;

                camera.Memory.Lock(lastId);
                try
                {
                    int w = 0, h = 0, pitch = 0;
                    camera.Memory.GetWidth(lastId, out w);
                    camera.Memory.GetHeight(lastId, out h);
                    camera.Memory.GetPitch(lastId, out pitch);

                    IntPtr ptr;
                    camera.Memory.ToIntPtr(lastId, out ptr);
                    if (ptr == IntPtr.Zero || w == 0 || h == 0) continue;

                    lastMean = ComputeMeanBrightness(ptr, w, h, pitch);
                    Console.WriteLine($"[IdsUEye] AutoExposure frame {i + 1}: meanBrightness={lastMean:F1}");

                    if (i >= MinConvergeFrames - 1 && lastMean < OverexposedThreshold)
                        break;
                }
                finally
                {
                    camera.Memory.Unlock(lastId);
                }
            }

            // ── 5. Lock the converged shutter (and any AWB that did not auto-disable) by disabling auto. ──
            if (shutterSupported)   camera.AutoFeatures.Sensor.Shutter.SetEnable(false);
            if (swShutterSupported) camera.AutoFeatures.Software.Shutter.SetEnable(false);
            if (swWhiteSupported)
            {
                try { camera.AutoFeatures.Software.WhiteBalance.SetEnable(false); } catch { }
            }
            if (sensorWhiteSupported)
            {
                try { camera.AutoFeatures.Sensor.Whitebalance.SetEnable(uEye.Defines.Whitebalance.WhiteBalanceMode.Disable); } catch { }
            }

            // Settle: let the locked exposure/white-balance stabilize before the real Freeze().
            Thread.Sleep(100);

            Console.WriteLine($"[IdsUEye] AutoExposure done. Final meanBrightness={lastMean:F1}");
        }

        /// <summary>Computes the mean brightness of a BGR8 native buffer (samples every 8th pixel for speed).</summary>
        private static unsafe double ComputeMeanBrightness(IntPtr ptr, int width, int height, int pitch)
        {
            byte* buf = (byte*)ptr.ToPointer();
            long sum = 0;
            long count = 0;
            const int step = 8; // sample every N pixels — fast enough for convergence check

            for (int row = 0; row < height; row += step)
            {
                byte* rowPtr = buf + row * pitch;
                for (int col = 0; col < width; col += step)
                {
                    int offset = col * 3;
                    // BGR: average R+G+B channels as overall luminance proxy
                    sum += rowPtr[offset] + rowPtr[offset + 1] + rowPtr[offset + 2];
                    count += 3;
                }
            }
            return count > 0 ? (double)sum / count : 128.0;
        }
        // ── END Auto-exposure convergence ────────────────────────────

        // ── DEBUG ONLY
        // Saves two files per capture to C:\Users\cocuy\Pictures\Feedback:
        //   ueye-<timestamp>.jpg  — raw BGR bytes from the uEye native buffer (no OpenCV processing)
        //   cv-<timestamp>.jpg    — the OpenCV Mat that the service returns to the stream
        private static readonly string DebugOutputDir = @"C:\Users\cocuy\Pictures\Feedback";

        private static unsafe void SaveDebugFrames(IntPtr nativeBuf, int width, int height, int pitch, Mat cvMat)
        {
            try
            {
                Directory.CreateDirectory(DebugOutputDir);
                var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");

                // 1. uEye raw: copy native buffer row by row (respecting pitch) into a fresh Mat and save.
                var ueyePath = Path.Combine(DebugOutputDir, $"ueye-{stamp}.jpg");
                using (var rawMat = new Mat(height, width, MatType.CV_8UC3))
                {
                    byte* src = (byte*)nativeBuf.ToPointer();
                    byte* dst = (byte*)rawMat.Data.ToPointer();
                    int rowBytes = width * 3;
                    for (int row = 0; row < height; row++)
                        System.Buffer.MemoryCopy(src + row * pitch, dst + row * rowBytes, rowBytes, rowBytes);
                    Cv2.ImWrite(ueyePath, rawMat);
                }
                Console.WriteLine($"[IdsUEye][DEBUG] Saved raw uEye frame → {ueyePath}");

                // 2. OpenCV result.
                var cvPath = Path.Combine(DebugOutputDir, $"cv-{stamp}.jpg");
                Cv2.ImWrite(cvPath, cvMat);
                Console.WriteLine($"[IdsUEye][DEBUG] Saved OpenCV frame  → {cvPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IdsUEye][DEBUG] SaveDebugFrames failed: {ex.Message}");
            }
        }
        // ── END DEBUG ────────────────────────────────────────────────────────

        public void Dispose()
        {
            _disposed = true;
        }
    }

    /// <summary>
    /// Selects which profile/parameter set to apply when initializing the uEye camera.
    /// Change <see cref="IdsUEyeCameraService.ActiveProfile"/> to switch profiles.
    /// </summary>
    public enum UEyeProfile
    {
        /// <summary>Factory defaults via camera.Parameter.ResetToDefault().</summary>
        Default,
        /// <summary>Parameter set stored inside the camera hardware (saved from uEye Cockpit).</summary>
        HardwareStored,
        /// <summary>Load a .ini file exported from uEye Cockpit. Set ProfileFilePath.</summary>
        FromFile,
        /// <summary>SceneMode.Automatic — general auto scene.</summary>
        SceneAutomatic,
        /// <summary>SceneMode.Portrait.</summary>
        ScenePortrait,
        /// <summary>SceneMode.Sunny — outdoor bright light.</summary>
        SceneSunny,
        /// <summary>SceneMode.Night — low light.</summary>
        SceneNight,
        /// <summary>SceneMode.Sports — fast movement.</summary>
        SceneSports,
        /// <summary>SceneMode.Landscape.</summary>
        SceneLandscape,
    }
}
