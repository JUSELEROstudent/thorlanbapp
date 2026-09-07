using GotsThorlabs.BLL;
using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Database.EntityRepo.Entities;
using GotsThorlabs.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GotsThorlabs.Services
{
    public class TourPlanningService : ITourPlanningService
    {
        private const int FallbackFrameWidth = 1920;
        private const int FallbackFrameHeight = 1080;

        private readonly ThorlabsDbContext _db;
        private readonly CameraServiceFactory _cameraFactory;

        public TourPlanningService(ThorlabsDbContext db, CameraServiceFactory cameraFactory)
        {
            _db = db;
            _cameraFactory = cameraFactory;
        }

        public async Task<TourTimeEstimator.Estimate> EstimateAsync(
            string groupCalibrationId, decimal areaXmm, decimal areaYmm, SweepPattern pattern, CancellationToken ct)
        {
            var groupId = groupCalibrationId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(groupId))
                throw new ArgumentException("Debe indicar el grupo de calibración.");
            if (areaXmm <= 0 || areaYmm <= 0)
                throw new ArgumentException("El área debe ser mayor que cero en ambos ejes.");

            var group = await _db.GroupCalibrations
                .AsNoTracking()
                .Include(g => g.Camera)
                .FirstOrDefaultAsync(g => g.GroupCailbrationId == groupId, ct);

            if (group == null)
                throw new KeyNotFoundException($"No existe el grupo de calibración {groupId}.");

            var calibrations = await _db.PicsCalibrations
                .AsNoTracking()
                .Where(p => p.GroupCailbrationId == groupId)
                .ToListAsync(ct);

            if (calibrations.Count == 0)
                throw new InvalidOperationException(
                    "El grupo no tiene calibración óptica: sin la relación píxeles/paso no se puede " +
                    "saber cuántas imágenes hacen falta ni, por tanto, cuánto tardará.");

            var motorCalibration = await _db.MotorCalibrations
                .AsNoTracking()
                .Include(m => m.AxisStepCalibrations)
                .Where(m => m.GroupCailbrationId == groupId)
                .OrderByDescending(m => m.Acepted)
                .ThenByDescending(m => m.Date)
                .FirstOrDefaultAsync(ct);

            var (frameWidth, frameHeight, probeFailed) = ProbeFrameSize(group.Camera);

            // Se reutiliza exactamente el mismo cálculo que ejecuta el recorrido, para que
            // la estimación no pueda divergir de lo que después ocurre de verdad.
            var grid = MosaicGridCalculator.Calculate(
                areaXmm, areaYmm, calibrations, frameWidth, frameHeight,
                ReadStepNm(motorCalibration, "x", forward: true),
                ReadStepNm(motorCalibration, "y", forward: true),
                ReadStepNm(motorCalibration, "y", forward: false));

            var estimate = TourTimeEstimator.Calculate(
                grid, pattern, motorCalibration?.StepRate ?? MotorMotion.DefaultStepRate);

            // Los problemas detectados al calcular el grid describen la calidad de la
            // calibración, no del recorrido, pero es aquí donde el operador los va a leer
            // antes de decidir si lanza el barrido.
            foreach (var advertencia in grid.Warnings)
                estimate.Warnings.Add(advertencia);

            if (motorCalibration == null)
            {
                estimate.Warnings.Add(
                    "El grupo no tiene caracterización mecánica del motor: se asume StepRate=200. " +
                    "Si el recorrido se ejecuta con otra velocidad, el tiempo real cambiará en proporción.");
            }

            if (probeFailed)
            {
                estimate.Warnings.Add(
                    $"No se pudo consultar el tamaño de cuadro de la cámara; se asume {FallbackFrameWidth}x{FallbackFrameHeight}. " +
                    "Si la resolución real es otra, el número de imágenes y el tiempo variarán.");
            }

            return estimate;
        }

        /// <summary>
        /// Toma un cuadro de sondeo para conocer la resolución, igual que hace TakeTour
        /// antes de calcular el grid. Se exige explícitamente Rows/Cols > 0 porque un
        /// driver que falla puede devolver un Mat válido pero vacío, y entonces el grid
        /// sale absurdo.
        /// </summary>
        private (int Width, int Height, bool Failed) ProbeFrameSize(Camera? camera)
        {
            if (camera == null) return (FallbackFrameWidth, FallbackFrameHeight, true);

            try
            {
                var service = _cameraFactory.GetService(camera.DriverType);
                service.ApplySettings(camera.LocalIdentifier, camera.SettingsJson);

                using var frame = service.CaptureFrame(camera.LocalIdentifier);
                if (frame != null && frame.Rows > 0 && frame.Cols > 0)
                    return (frame.Cols, frame.Rows, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TourPlanning] No se pudo sondear la cámara: {ex.Message}");
            }

            return (FallbackFrameWidth, FallbackFrameHeight, true);
        }

        private static double? ReadStepNm(MotorCalibration? calibration, string axisName, bool forward)
        {
            var axis = calibration?.AxisStepCalibrations
                .FirstOrDefault(a => string.Equals(a.AxisName, axisName, StringComparison.OrdinalIgnoreCase));

            if (axis == null || axis.Status != "complete") return null;

            var raw = forward ? axis.StepSizeNmForward : axis.StepSizeNmBackward;
            return StepSizeCalculator.TryParse(raw, out var value) && value > 0 ? value : null;
        }
    }
}
