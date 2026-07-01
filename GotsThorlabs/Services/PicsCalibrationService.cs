using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Database.EntityRepo.Entities;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using OpenCvSharp;
using Thorlabs.MotionControl.DeviceManagerCLI;
using Thorlabs.MotionControl.KCube.InertialMotorCLI;

namespace GotsThorlabs.Services
{
    public class PicsCalibrationService : IPicsCalibrationService
    {
        private readonly ThorlabsDbContext _db;
        private readonly string _imagesBasePath;
        private readonly CameraServiceFactory _cameraFactory;

        public PicsCalibrationService(ThorlabsDbContext db, IWebHostEnvironment env, IConfiguration configuration, CameraServiceFactory cameraFactory)
        {
            _db = db;
            _imagesBasePath = configuration["CalibrationImagesPath"]
                ?? Path.Combine(env.ContentRootPath, "StaticFiles", "pics-calibrations");
            _cameraFactory = cameraFactory;
        }

        public async Task<IEnumerable<PicsCalibration>> GetAllAsync(CancellationToken ct)
        {
            return await _db.PicsCalibrations.AsNoTracking().ToListAsync(ct);
        }

        public async Task<PicsCalibration?> GetByIdAsync(string id, CancellationToken ct)
        {
            return await _db.PicsCalibrations.AsNoTracking().FirstOrDefaultAsync(p => p.PicsCalibrationId == id, ct);
        }

        public async Task<PicsCalibration> CreateAsync(PicsCalibrationUploadDTO dto, CancellationToken ct)
        {
            if (dto.Pic1File is null || dto.Pic2File is null)
                throw new ArgumentException("Debe enviar Pic1File y Pic2File.");

            var id = Guid.NewGuid().ToString();
            var recordDir = Path.Combine(_imagesBasePath, id);
            Directory.CreateDirectory(recordDir);

            var pic1Path = SaveFile(dto.Pic1File, recordDir, "pic1");
            var pic2Path = SaveFile(dto.Pic2File, recordDir, "pic2");

            var entity = new PicsCalibration
            {
                PicsCalibrationId = id,
                GroupCailbrationId = dto.GroupCailbrationId,
                Pic1 = pic1Path,
                Pic2 = pic2Path,
                AxeDirectionCalibration = dto.AxeDirectionCalibration,
                Acepted = dto.Acepted ? 1 : 0   ,
                Dx = dto.Dx,
                Dy = dto.Dy,
                Confidence = dto.Confidence,
                MeasureUnit = dto.MeasureUnit,
                MovementValue = dto.MovementValue
            };

            _db.PicsCalibrations.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity;
        }

        public async Task<PicsCalibration> UploadAsync(PicsCalibrationUploadDTO dto, CancellationToken ct)
        {
            if (dto.Pic1File is null || dto.Pic2File is null)
                throw new ArgumentException("Debe enviar Pic1File y Pic2File.");

            var id = Guid.NewGuid().ToString();
            var recordDir = Path.Combine(_imagesBasePath, id);
            Directory.CreateDirectory(recordDir);

            SaveFile(dto.Pic1File, recordDir, "pic1");
            SaveFile(dto.Pic2File, recordDir, "pic2");

            var baseRequestPath = "/SouerceStaticFiles/pics-calibrations/" + id;
            var pic1Url = baseRequestPath + "/pic1" + Path.GetExtension(dto.Pic1File.FileName);
            var pic2Url = baseRequestPath + "/pic2" + Path.GetExtension(dto.Pic2File.FileName);

            var entity = new PicsCalibration
            {
                PicsCalibrationId = id,
                GroupCailbrationId = dto.GroupCailbrationId,
                Pic1 = pic1Url,
                Pic2 = pic2Url,
                AxeDirectionCalibration = dto.AxeDirectionCalibration,
                Acepted = dto.Acepted ? 1 : 0,
                Dx = dto.Dx,
                Dy = dto.Dy,
                Confidence = dto.Confidence,
                MeasureUnit = dto.MeasureUnit,
                MovementValue = dto.MovementValue
            };

            _db.PicsCalibrations.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity;
        }

        public async Task<List<PicsCalibration>> RunAutoCalibrationAsync(
            string kimDeviceId,
            string groupCalibrationId,
            string axis,
            ICameraService cameraService,
            string? localIdentifier,
            IPhaseCorrelationService phaseCorrelationService,
            CancellationToken ct)
        {
            var calibrationSteps = new[] { 0, 1, 10, 100,1000 };
            var results = new List<PicsCalibration>();

            var device = KCubeInertialMotor.CreateKCubeInertialMotor(kimDeviceId);
            try
            {
                device.Connect(kimDeviceId);
            }
            catch (Exception ex)
            {
                device.Disconnect(true);
                throw new InvalidOperationException($"Error al conectar con el dispositivo KIM: {ex.Message}");
            }

            if (!device.IsSettingsInitialized())
            {
                try
                {
                    const int timeoutMs = 5000; // increased timeout
                    const int attempts = 3;
                    var ok = false;
                    for (int i = 0; i < attempts && !ok; i++)
                    {
                        try
                        {
                            device.WaitForSettingsInitialized(timeoutMs);
                            ok = device.IsSettingsInitialized();
                        }
                        catch (DeviceSettingsException dex)
                        {
                            // optional small delay before retry
                            Thread.Sleep(200);
                            if (i == attempts - 1)
                            {
                                device.Disconnect(true);
                                throw new InvalidOperationException(
                                    $"KIM settings failed to initialize after {attempts} attempts: {dex.Message}", dex);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    device.Disconnect(true);
                    throw;
                }
                //try
                //{
                //    device.WaitForSettingsInitialized(500);
                //}
                //catch
                //{
                //    device.Disconnect(true);
                //    throw new InvalidOperationException("El dispositivo KIM no inicializó correctamente.");
                //}
            }
            var currentGroupCalibration = _db.GroupCalibrations.Where(g => g.GroupCailbrationId == groupCalibrationId)
                .Include(g=> g.Camera)
                .FirstOrDefault();

            if (currentGroupCalibration == null)
            {
                device.StopPolling();
                device.Disconnect(true);
                throw new InvalidOperationException($"No se encontró el grupo de calibración con ID {groupCalibrationId}.");
            }

            if (currentGroupCalibration.Camera == null)
            {
                device.StopPolling();
                device.Disconnect(true);
                throw new InvalidOperationException($"El grupo de calibración {groupCalibrationId} no tiene una cámara asociada.");
            }

            var driverType = currentGroupCalibration.Camera.DriverType ?? "generic";
            var resolvedCameraService = _cameraFactory.GetService(driverType);

            device.StartPolling(250);
            Thread.Sleep(500);
            device.EnableDevice();
            Thread.Sleep(500);

            var channel = axis.ToLower() == "x"
                ? InertialMotorStatus.MotorChannels.Channel1
                : InertialMotorStatus.MotorChannels.Channel2;

            var calibrationFolderName = $"calibration_{DateTime.Now:yyyyMMdd_HHmmss}";
            var calibrationFolderPath = Path.Combine(_imagesBasePath, calibrationFolderName);
            Directory.CreateDirectory(calibrationFolderPath);

            var deviceConnected = true;
            try
            {
                foreach (var step in calibrationSteps)
                {
                    ct.ThrowIfCancellationRequested();

                    try
                    {
                        MoveMotor(device, channel, 0);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Error al mover el motor KIM a la posición 0 antes del paso {step}: {ex.Message}", ex);
                    }

                    Thread.Sleep(300);

                    Mat frame1 = null;
                    try
                    {
                        frame1 = resolvedCameraService.CaptureFrame(currentGroupCalibration.Camera.LocalIdentifier);
                        if (frame1 == null || frame1.Empty())
                        {
                            throw new InvalidOperationException($"Error al capturar la imagen de referencia (posición 0) para el paso {step}. La cámara (driver: {driverType}, identifier: {currentGroupCalibration.Camera.LocalIdentifier}) devolvió un frame vacío.");
                        }

                        var pic1Path = Path.Combine(calibrationFolderPath, $"step_{step}_pic1.jpg");
                        frame1.SaveImage(pic1Path);
                    }
                    catch (Exception ex) when (ex is not InvalidOperationException)
                    {
                        throw new InvalidOperationException($"Error al capturar la imagen de referencia (posición 0) para el paso {step} con la cámara (driver: {driverType}, identifier: {currentGroupCalibration.Camera.LocalIdentifier}): {ex.Message}", ex);
                    }
                    finally
                    {
                        frame1?.Dispose();
                    }

                    Thread.Sleep(200);

                    try
                    {
                        MoveMotor(device, channel, step);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Error al mover el motor KIM al paso {step} desde posición 0: {ex.Message}", ex);
                    }

                    Thread.Sleep(300);

                    Mat frame2 = null;
                    try
                    {
                        frame2 = resolvedCameraService.CaptureFrame(currentGroupCalibration.Camera.LocalIdentifier);
                        if (frame2 == null || frame2.Empty())
                        {
                            throw new InvalidOperationException($"Error al capturar la imagen después del movimiento al paso {step}. La cámara (driver: {driverType}, identifier: {currentGroupCalibration.Camera.LocalIdentifier}) devolvió un frame vacío.");
                        }

                        var pic2Path = Path.Combine(calibrationFolderPath, $"step_{step}_pic2.jpg");
                        frame2.SaveImage(pic2Path);
                    }
                    catch (Exception ex) when (ex is not InvalidOperationException)
                    {
                        throw new InvalidOperationException($"Error al capturar la imagen después del movimiento al paso {step} con la cámara (driver: {driverType}, identifier: {currentGroupCalibration.Camera.LocalIdentifier}): {ex.Message}", ex);
                    }
                    finally
                    {
                        frame2?.Dispose();
                    }

                    var phaseResult = phaseCorrelationService.DetectShiftFromPaths(
                        Path.Combine(calibrationFolderPath, $"step_{step}_pic1.jpg"),
                        Path.Combine(calibrationFolderPath, $"step_{step}_pic2.jpg"));

                    var calibrationRecord = new PicsCalibration
                    {
                        PicsCalibrationId = Guid.NewGuid().ToString(),
                        GroupCailbrationId = groupCalibrationId,
                        Pic1 = Path.Combine(calibrationFolderPath, $"step_{step}_pic1.jpg"),
                        Pic2 = Path.Combine(calibrationFolderPath, $"step_{step}_pic2.jpg"),
                        AxeDirectionCalibration = axis,
                        Acepted = 0,
                        Dx = phaseResult.Dx.ToString("F6"),
                        Dy = phaseResult.Dy.ToString("F6"),
                        Confidence = phaseResult.Confidence.ToString("F6"),
                        MeasureUnit = "pixels",
                        MovementValue = step.ToString(),
                        NumberOfSteps = step,
                        AxisMovementName = axis
                    };

                    _db.PicsCalibrations.Add(calibrationRecord);
                    results.Add(calibrationRecord);
                }

                await _db.SaveChangesAsync(ct);

                foreach (var record in results)
                {
                    record.GroupCailbration = null;
                }
            }
            catch (OperationCanceledException)
            {
                throw new InvalidOperationException("La calibración automática fue cancelada.");
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error inesperado durante la calibración automática en el paso actual: {ex.Message}", ex);
            }
            finally
            {
                if (deviceConnected)
                {
                    try
                    {
                        device.StopPolling();
                        device.Disconnect(true);
                    }
                    catch
                    {
                    }
                }

                // Release the camera connection so the device is free for the
                // vendor's own software (e.g. IDS Cockpit). For drivers that
                // open/close per capture (generic / ids_peak) this is a no-op;
                // for the persistent uEye session it closes the live connection.
                try
                {
                    resolvedCameraService?.ReleaseConnection();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PicsCalibration] ReleaseConnection failed: {ex.Message}");
                }
            }

            return results;
        }

        private static void MoveMotor(KCubeInertialMotor device, InertialMotorStatus.MotorChannels channel, int position)
        {
            if (device.GetPosition(channel) == position)
                return;

            // Start the move (timeout 0 = non-blocking, returns immediately).
            device.MoveTo(channel, position, 0);

            // Poll the actual position until the motor reaches the target.
            // The inertial motor moves by applying vibration pulses; the position
            // counter only increments as the stage physically moves. We wait
            // however long it takes — no fixed timeout — so the camera never
            // captures before the stage has arrived.
            const int pollIntervalMs = 100;
            const int settleMs       = 500;  // extra settle after arriving (let vibrations damp)
            const int safetyMaxMs    = 120_000; // 2 min safety valve

            var elapsed = 0;
            while (device.GetPosition(channel) != position)
            {
                Thread.Sleep(pollIntervalMs);
                elapsed += pollIntervalMs;
                if (elapsed >= safetyMaxMs)
                    throw new InvalidOperationException(
                        $"El motor no alcanzó la posición {position} tras {safetyMaxMs / 1000}s " +
                        $"(posición actual: {device.GetPosition(channel)}).");
            }

            // The stage arrived — let any residual vibration dampen before capture.
            Thread.Sleep(settleMs);
        }

        public async Task UpdateAsync(string id, PicsCalibrationUpdateDTO dto, CancellationToken ct)
        {
            var existing = await _db.PicsCalibrations.FirstOrDefaultAsync(p => p.PicsCalibrationId == id, ct);
            if (existing is null) throw new KeyNotFoundException($"PicsCalibration {id} not found.");

            existing.GroupCailbrationId = dto.GroupCailbrationId;
            existing.Pic1 = dto.Pic1;
            existing.Pic2 = dto.Pic2;
            existing.AxeDirectionCalibration = dto.AxeDirectionCalibration;
            existing.Acepted = dto.Acepted;
            existing.Dx = dto.Dx;
            existing.Dy = dto.Dy;
            existing.Confidence = dto.Confidence;
            existing.MeasureUnit = dto.MeasureUnit;
            existing.MovementValue = dto.MovementValue;

            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(string id, CancellationToken ct)
        {
            var pic = await _db.PicsCalibrations.FirstOrDefaultAsync(p => p.PicsCalibrationId == id, ct);
            if (pic is null) throw new KeyNotFoundException($"PicsCalibration {id} not found.");

            _db.PicsCalibrations.Remove(pic);
            await _db.SaveChangesAsync(ct);
        }

        private static string SaveFile(IFormFile file, string recordDir, string name)
        {
            var ext = Path.GetExtension(file.FileName);
            var fullPath = Path.Combine(recordDir, name + ext);
            using var stream = System.IO.File.Create(fullPath);
            file.CopyTo(stream);
            return fullPath;
        }

        public async Task<IEnumerable<PicsCalibration>> GetByGroupCalibrationIdAsync(string groupCalibrationId, CancellationToken ct)
        {
            var response= await _db.PicsCalibrations.AsNoTracking()
                .Where(p => p.GroupCailbrationId == groupCalibrationId)
                .ToListAsync(ct);
            return response;
        }
    }
}
