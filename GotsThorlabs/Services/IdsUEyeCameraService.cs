using uEye;
using uEye.Defines;
using uEye.Types;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using OpenCvSharp;
using System.Collections.Generic;
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

        // ── PERSISTENT SESSION ────────────────────────────────────────
        // The camera is opened & calibrated ONCE per identifier, then reused
        // for every subsequent Freeze() capture. This avoids re-running the
        // ~30-frame auto-exposure/AWB convergence on every single image and
        // keeps the calibration stable across the whole calibration flow.
        private readonly SemaphoreSlim _sessionLock = new SemaphoreSlim(1, 1);
        private Camera? _sessionCamera;
        private string? _sessionIdentifier;   // identifier the live session is bound to
        private int    _sessionMemoryId;        // capture buffer of the live session

        // How long to let the camera settle after calibration before the first
        // real capture (lets the locked exposure/WB fully stabilize).
        private const int SettleAfterCalibrationMs = 3000;

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
            => CaptureFrame(cameraId.ToString());

        public Mat CaptureFrame(string localIdentifier)
        {
            if (string.IsNullOrWhiteSpace(localIdentifier))
                return new Mat();

            // Serialize all captures through the single live session. The first call
            // for an identifier opens+calibrates the camera (heavy), every later call
            // just does a fast Freeze(). Different identifiers re-open the session.
            _sessionLock.Wait();
            try
            {
                return CaptureWithPersistentSession(localIdentifier);
            }
            finally
            {
                _sessionLock.Release();
            }
        }

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

        // ── Persistent-session capture ────────────────────────────────
        // Opens & calibrates the camera the first time an identifier is seen,
        // then reuses the live connection for every subsequent Freeze().
        private Mat CaptureWithPersistentSession(string localIdentifier)
        {
            // If the requested identifier differs from the live session, rebuild it.
            if (_sessionCamera == null || !string.Equals(_sessionIdentifier, localIdentifier, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[IdsUEye] No active session for '{localIdentifier}', opening...");
                if (!OpenAndCalibrate(localIdentifier))
                {
                    Console.WriteLine($"[IdsUEye] OpenAndCalibrate FAILED for '{localIdentifier}'. Returning empty Mat.");
                    return new Mat();
                }
            }

            var camera = _sessionCamera!;
            try
            {
                return FreezeAndCopy(camera);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IdsUEye] FreezeAndCopy threw exception, will re-open next call: {ex.Message}");
                CloseSession();
                return new Mat();
            }
        }

        /// <summary>
        /// Opens the uEye device for <paramref name="localIdentifier"/>, applies the profile,
        /// locks the BGR8 pixel format, converges exposure + white balance and lets the
        /// camera settle. The live <see cref="Camera"/> + buffer are cached as the active
        /// session so later captures skip all of this and just Freeze().
        /// </summary>
        private bool OpenAndCalibrate(string localIdentifier)
        {
            // Close any previously held session first (different camera).
            CloseSession();

            CameraInformation[]? cameraList;
            uEye.Info.Camera.GetCameraList(out cameraList);
            if (cameraList == null || cameraList.Length == 0)
            {
                Console.WriteLine("[IdsUEye] No uEye cameras found.");
                return false;
            }

            int deviceId = ResolveDeviceId(cameraList, localIdentifier);
            if (deviceId < 0)
            {
                Console.WriteLine($"[IdsUEye] Could not resolve device for identifier '{localIdentifier}'.");
                return false;
            }

            var camera = new Camera();
            Status status = camera.Init(deviceId);
            Console.WriteLine($"[IdsUEye] Init(deviceId={deviceId}) = {status}");
            if (status != Status.Success)
            {
                Console.WriteLine($"[IdsUEye] Init failed — camera may be in use by another application (e.g. IDS Cockpit). Close it and retry.");
                return false;
            }

            try
            {
                // Apply profile FIRST (it can change the pixel format).
                ApplyProfile(camera);

                // Lock BGR8 output AFTER the profile so the color conversion persists.
                var pixStatus = camera.PixelFormat.Set(ColorMode.BGR8Packed);
                Console.WriteLine($"[IdsUEye] PixelFormat.Set(BGR8Packed) = {pixStatus}");

                // Allocate a SINGLE capture buffer for snapshot (Freeze) mode.
                int memId;
                var allocStatus = camera.Memory.Allocate(out memId, true);
                Console.WriteLine($"[IdsUEye] Memory.Allocate = {allocStatus}, memId = {memId}");
                if (allocStatus != Status.Success)
                {
                    Console.WriteLine($"[IdsUEye] Memory.Allocate failed: {allocStatus}");
                    camera.Exit();
                    return false;
                }

                // Freerun so Freeze() returns the next ready frame instead of blocking.
                var triggerStatus = camera.Trigger.Set(TriggerMode.Off);
                Console.WriteLine($"[IdsUEye] Trigger.Set(Off) = {triggerStatus}");

                // Converge exposure (auto-shutter only, no auto-gain) + white balance.
                Console.WriteLine("[IdsUEye] Starting auto-exposure/AWB convergence...");
                ApplyAutoExposure(camera);

                // Let the locked exposure / AWB stabilize before the first real capture.
                Console.WriteLine($"[IdsUEye] Calibrated. Settling {SettleAfterCalibrationMs} ms before first capture...");
                Thread.Sleep(SettleAfterCalibrationMs);

                _sessionCamera     = camera;
                _sessionIdentifier = localIdentifier;
                _sessionMemoryId   = memId;
                Console.WriteLine($"[IdsUEye] Session opened & calibrated for '{localIdentifier}'.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IdsUEye] Calibration failed for '{localIdentifier}': {ex.Message}");
                Console.WriteLine($"[IdsUEye] Stack: {ex.StackTrace}");
                try { camera.Exit(); } catch { }
                return false;
            }
        }

        /// <summary>Fast-path: takes one frame from the already-open session and copies it to a Mat.</summary>
        private Mat FreezeAndCopy(Camera camera)
        {
            // Simple snapshot approach: Freeze(DeviceParameter.Wait) captures ONE
            // fresh frame and blocks until complete. This is the pattern used by
            // the SDK cockpit sample for "snapshot" mode and is the most reliable
            // way to get a single frame. No Capture()/Stop() dance needed.
            var freezeStatus = camera.Acquisition.Freeze(DeviceParameter.Wait);
            if (freezeStatus != Status.Success)
            {
                Console.WriteLine($"[IdsUEye] FreezeAndCopy: Freeze() failed = {freezeStatus}");
                return new Mat();
            }

            // Read the completed buffer.
            int lastMemId;
            var getLastStatus = camera.Memory.GetLast(out lastMemId);
            if (getLastStatus != Status.Success || lastMemId <= 0)
            {
                Console.WriteLine($"[IdsUEye] FreezeAndCopy: GetLast failed = {getLastStatus}, memId = {lastMemId}");
                return new Mat();
            }

            camera.Memory.Lock(lastMemId);

            int width = 0, height = 0, pitch = 0;
            camera.Memory.GetWidth(lastMemId, out width);
            camera.Memory.GetHeight(lastMemId, out height);
            camera.Memory.GetPitch(lastMemId, out pitch);

            if (width == 0 || height == 0)
            {
                camera.Memory.Unlock(lastMemId);
                Console.WriteLine($"[IdsUEye] FreezeAndCopy: Empty dimensions (w={width} h={height}).");
                return new Mat();
            }

            IntPtr bufPtr;
            camera.Memory.ToIntPtr(lastMemId, out bufPtr);

            Console.WriteLine($"[IdsUEye] FreezeAndCopy: w={width} h={height} pitch={pitch}");

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

            if (mat.Empty())
                Console.WriteLine($"[IdsUEye] FreezeAndCopy: Mat empty after copy!");
            else
                Console.WriteLine($"[IdsUEye] FreezeAndCopy: OK {width}x{height}.");

            return mat;
        }

        private void CloseSession()
        {
            if (_sessionCamera != null)
            {
                var cam = _sessionCamera;

                // 1) Stop any active acquisition.
                try
                {
                    bool started;
                    cam.Acquisition.HasStarted(out started);
                    if (started) cam.Acquisition.Stop();
                }
                catch (Exception ex) { Console.WriteLine($"[IdsUEye] CloseSession Stop: {ex.Message}"); }

                // 2) Free all allocated memory buffers.
                try
                {
                    int[] idList;
                    if (cam.Memory.GetList(out idList) == Status.Success && idList != null)
                    {
                        foreach (var memId in idList)
                        {
                            try { cam.Memory.Free(memId); } catch { }
                        }
                    }
                }
                catch (Exception ex) { Console.WriteLine($"[IdsUEye] CloseSession Free: {ex.Message}"); }

                // 3) Release the device handle so IDS Cockpit / other apps can use it.
                try { cam.Exit(); } catch { }

                _sessionCamera = null;
            }
            _sessionIdentifier = null;
            _sessionMemoryId   = 0;
        }

        /// <summary>
        /// Releases the live uEye session so the camera is free for use by other
        /// software (e.g. IDS Cockpit). Call this when a capture flow finishes.
        /// Safe to call even if no session is open.
        /// </summary>
        public void ReleaseConnection()
        {
            _sessionLock.Wait();
            try
            {
                if (_sessionCamera != null)
                    Console.WriteLine($"[IdsUEye] Releasing connection for '{_sessionIdentifier}'.");
                CloseSession();
            }
            finally
            {
                _sessionLock.Release();
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
            // The software AWB (ActivateMode.Once) converges the WB gains then
            // auto-disables, locking the color balance. We rely solely on the
            // software path — it is the one confirmed working by the SDK sample.
            bool swWhiteSupported = false;
            camera.AutoFeatures.Software.WhiteBalance.GetSupported(out swWhiteSupported);
            if (swWhiteSupported)
            {
                try { camera.AutoFeatures.Software.WhiteBalance.SetEnable(uEye.Defines.ActivateMode.Once); }
                catch (Exception ex) { Console.WriteLine($"[IdsUEye] AWB Once failed: {ex.Message}"); }
            }

            Console.WriteLine($"[IdsUEye] AutoExposure setup: gainAutoSensor={gainSupported} gainAutoSW={swGainSupported} awbSW={swWhiteSupported}");

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
                if (i == 0)
                    Console.WriteLine($"[IdsUEye] AutoExposure: first Freeze() = {st}");
                if (st != Status.Success)
                {
                    Console.WriteLine($"[IdsUEye] AutoExposure: Freeze() failed at frame {i + 1}: {st}");
                    break;
                }

                int lastId;
                camera.Memory.GetLast(out lastId);
                if (i == 0)
                    Console.WriteLine($"[IdsUEye] AutoExposure: first GetLast() memId = {lastId}");
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

            // Settle: let the locked exposure/white-balance stabilize before the real Freeze().
            Thread.Sleep(100);

            // Stop any active acquisition so the session starts from a clean state.
            // The Freeze() calls above leave the camera STOPPED, but calling Stop()
            // explicitly guarantees a clean transition to Capture() later.
            try
            {
                bool started;
                camera.Acquisition.HasStarted(out started);
                if (started) camera.Acquisition.Stop();
            }
            catch { }

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
            if (_disposed) return;
            _disposed = true;

            CloseSession();
            _sessionLock.Dispose();
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
