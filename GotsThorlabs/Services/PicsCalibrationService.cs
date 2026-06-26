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

        public PicsCalibrationService(ThorlabsDbContext db, IWebHostEnvironment env, IConfiguration configuration)
        {
            _db = db;
            _imagesBasePath = configuration["CalibrationImagesPath"]
                ?? Path.Combine(env.ContentRootPath, "StaticFiles", "pics-calibrations");
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
            var calibrationSteps = new[] { 1, 10, 1000 };
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

            try
            {
                foreach (var step in calibrationSteps)
                {
                    ct.ThrowIfCancellationRequested();

                    var moveSuccess = MoveMotor(device, channel, step);
                    if (!moveSuccess)
                    {
                        device.StopPolling();
                        device.Disconnect(true);
                        throw new InvalidOperationException($"Error al mover el motor al paso {step}.");
                    }

                    Thread.Sleep(300);

                    using var frame1 = cameraService.CaptureFrame(currentGroupCalibration.Camera.LocalIdentifier);
                    if (frame1 == null || frame1.Empty())
                        throw new InvalidOperationException($"Error al capturar la primera imagen en el paso {step}.");

                    var pic1Path = Path.Combine(calibrationFolderPath, $"step_{step}_pic1.jpg");
                    frame1.SaveImage(pic1Path);

                    Thread.Sleep(200);

                    using var frame2 = cameraService.CaptureFrame(currentGroupCalibration.Camera.LocalIdentifier);
                    if (frame2 == null || frame2.Empty())
                        throw new InvalidOperationException($"Error al capturar la segunda imagen en el paso {step}.");

                    var pic2Path = Path.Combine(calibrationFolderPath, $"step_{step}_pic2.jpg");
                    frame2.SaveImage(pic2Path);

                    var phaseResult = phaseCorrelationService.DetectShiftFromPaths(pic1Path, pic2Path);

                    var calibrationRecord = new PicsCalibration
                    {
                        PicsCalibrationId = Guid.NewGuid().ToString(),
                        GroupCailbrationId = groupCalibrationId,
                        Pic1 = pic1Path,
                        Pic2 = pic2Path,
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
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }
            finally
            {
                device.StopPolling();
                device.Disconnect(true);
            }

            return results;
        }

        private static bool MoveMotor(KCubeInertialMotor device, InertialMotorStatus.MotorChannels channel, int position)
        {
            if (device.GetPosition(channel) == position)
                return true;

            try
            {
                device.MoveTo(channel, position, 6000);
                return true;
            }
            catch
            {
                return false;
            }
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
