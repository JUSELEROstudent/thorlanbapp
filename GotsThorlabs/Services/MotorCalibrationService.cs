using System.Globalization;
using GotsThorlabs.BLL;
using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Database.EntityRepo.Entities;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using Microsoft.EntityFrameworkCore;
using Thorlabs.MotionControl.DeviceManagerCLI;
using Thorlabs.MotionControl.KCube.InertialMotorCLI;

namespace GotsThorlabs.Services
{
    public class MotorCalibrationService : IMotorCalibrationService
    {
        /// <summary>
        /// Diferencia relativa entre sentidos a partir de la cual se advierte. No bloquea:
        /// por encima de este valor el patrón "S" del recorrido merece revisión, pero el
        /// sistema sigue operando igual.
        /// </summary>
        public const double HysteresisWarningPct = 5.0;

        private static readonly string[] Axes = { "x", "y" };

        private readonly ThorlabsDbContext _db;

        public MotorCalibrationService(ThorlabsDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<MotorCalibrationDTO>> GetByGroupAsync(string groupCalibrationId, CancellationToken ct)
        {
            var items = await Query()
                .Where(m => m.GroupCailbrationId == groupCalibrationId)
                .ToListAsync(ct);

            return items
                .OrderByDescending(m => m.Acepted)
                .ThenByDescending(m => m.Date, StringComparer.Ordinal)
                .Select(ToDto)
                .ToList();
        }

        public async Task<MotorCalibrationDTO?> GetByIdAsync(string id, CancellationToken ct)
        {
            var entity = await Query().FirstOrDefaultAsync(m => m.MotorCalibrationId == id, ct);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<MotorCalibrationDTO> CreateAsync(MotorCalibrationCreateDTO dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.GroupCailbrationId))
                throw new ArgumentException("Debe indicar el grupo de calibración.");
            if (string.IsNullOrWhiteSpace(dto.KimDeviceId))
                throw new ArgumentException("Debe indicar el dispositivo KIM.");

            var groupExists = await _db.GroupCalibrations
                .AnyAsync(g => g.GroupCailbrationId == dto.GroupCailbrationId, ct);
            if (!groupExists)
                throw new KeyNotFoundException($"No existe el grupo de calibración {dto.GroupCailbrationId}.");

            var entity = new MotorCalibration
            {
                MotorCalibrationId = Guid.NewGuid().ToString(),
                GroupCailbrationId = dto.GroupCailbrationId,
                KimDeviceId = dto.KimDeviceId.Trim(),
                StepRate = dto.StepRate,
                StepAcceleration = dto.StepAcceleration,
                Status = "in_progress",
                Acepted = 0,
                Date = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                AditionalInfo = dto.AditionalInfo
            };

            // Ambos ejes se crean de entrada, vacíos: así la vista sabe qué falta por medir
            // sin tener que inventarse la estructura.
            foreach (var axis in Axes)
            {
                entity.AxisStepCalibrations.Add(new AxisStepCalibration
                {
                    AxisStepCalibrationId = Guid.NewGuid().ToString(),
                    MotorCalibrationId = entity.MotorCalibrationId,
                    AxisName = axis,
                    Status = "in_progress"
                });
            }

            _db.MotorCalibrations.Add(entity);
            await _db.SaveChangesAsync(ct);

            return ToDto(entity);
        }

        public async Task<AxisStepCalibrationDTO> RecordMeasurementAsync(
            string motorCalibrationId, AxisStepMeasurementCreateDTO dto, CancellationToken ct)
        {
            var axis = await LoadAxisAsync(motorCalibrationId, dto.AxisName, ct);
            var direction = NormalizeDirection(dto.Direction);

            // Volver a medir una parada es corregirla, no añadir un punto: si ya hay una
            // lectura para esa combinación de sentido y orden, se reemplaza. De lo
            // contrario la regresión acumularía dos puntos contradictorios para el mismo
            // desplazamiento.
            var existing = axis.AxisStepMeasurements
                .FirstOrDefault(m => m.Direction == direction && m.Sequence == dto.Sequence);

            if (existing != null)
            {
                existing.StepsCommanded = dto.StepsCommanded;
                existing.CaliperReadingMm = StepSizeCalculator.Format(dto.CaliperReadingMm);
                existing.MeasuredAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            }
            else
            {
                axis.AxisStepMeasurements.Add(new AxisStepMeasurement
                {
                    AxisStepMeasurementId = Guid.NewGuid().ToString(),
                    AxisStepCalibrationId = axis.AxisStepCalibrationId,
                    Direction = direction,
                    Sequence = dto.Sequence,
                    StepsCommanded = dto.StepsCommanded,
                    CaliperReadingMm = StepSizeCalculator.Format(dto.CaliperReadingMm),
                    MeasuredAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
                });
            }

            Recalculate(axis);
            await _db.SaveChangesAsync(ct);
            await RefreshParentStatusAsync(motorCalibrationId, ct);

            return ToDto(axis);
        }

        public async Task<AxisStepCalibrationDTO> UpdateMeasurementAsync(
            string measurementId, AxisStepMeasurementUpdateDTO dto, CancellationToken ct)
        {
            var measurement = await _db.AxisStepMeasurements
                .FirstOrDefaultAsync(m => m.AxisStepMeasurementId == measurementId, ct);
            if (measurement == null)
                throw new KeyNotFoundException($"No existe la medición {measurementId}.");

            measurement.CaliperReadingMm = StepSizeCalculator.Format(dto.CaliperReadingMm);
            measurement.MeasuredAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

            var axis = await LoadAxisByIdAsync(measurement.AxisStepCalibrationId, ct);
            Recalculate(axis);
            await _db.SaveChangesAsync(ct);
            await RefreshParentStatusAsync(axis.MotorCalibrationId, ct);

            return ToDto(axis);
        }

        public async Task<AxisStepCalibrationDTO> DeleteMeasurementAsync(string measurementId, CancellationToken ct)
        {
            var measurement = await _db.AxisStepMeasurements
                .FirstOrDefaultAsync(m => m.AxisStepMeasurementId == measurementId, ct);
            if (measurement == null)
                throw new KeyNotFoundException($"No existe la medición {measurementId}.");

            var axisId = measurement.AxisStepCalibrationId;
            _db.AxisStepMeasurements.Remove(measurement);
            await _db.SaveChangesAsync(ct);

            var axis = await LoadAxisByIdAsync(axisId, ct);
            Recalculate(axis);
            await _db.SaveChangesAsync(ct);
            await RefreshParentStatusAsync(axis.MotorCalibrationId, ct);

            return ToDto(axis);
        }

        public async Task MoveAsync(string motorCalibrationId, MotorMoveDTO dto, CancellationToken ct)
        {
            var calibration = await _db.MotorCalibrations
                .FirstOrDefaultAsync(m => m.MotorCalibrationId == motorCalibrationId, ct);
            if (calibration == null)
                throw new KeyNotFoundException($"No existe la caracterización {motorCalibrationId}.");

            var channel = ResolveChannel(dto.AxisName);

            var device = KCubeInertialMotor.CreateKCubeInertialMotor(calibration.KimDeviceId);
            try
            {
                device.Connect(calibration.KimDeviceId);

                if (!device.IsSettingsInitialized())
                    device.WaitForSettingsInitialized(5000);

                device.StartPolling(250);
                Thread.Sleep(500);
                device.EnableDevice();
                Thread.Sleep(500);

                // Se mueve con los MISMOS parámetros que se están caracterizando: el
                // tamaño de paso de un actuador inercial depende de la velocidad y la
                // aceleración, así que medirlo accionando de otro modo daría un número que
                // no aplica al recorrido real.
                var config = device.GetInertialMotorConfiguration(calibration.KimDeviceId);
                var settings = ThorlabsInertialMotorSettings.GetSettings(config);
                foreach (var ch in new[] { InertialMotorStatus.MotorChannels.Channel1, InertialMotorStatus.MotorChannels.Channel2 })
                {
                    settings.Drive.Channel(ch).StepRate = (int)calibration.StepRate;
                    settings.Drive.Channel(ch).StepAcceleration = (int)calibration.StepAcceleration;
                }
                device.SetSettings(settings, true, true);

                MoveAndWait(device, channel, dto.TargetSteps, ct);
            }
            finally
            {
                try { device.StopPolling(); } catch { /* el cierre no debe enmascarar el error real */ }
                try { device.Disconnect(true); } catch { }
            }
        }

        public async Task AcceptAsync(string motorCalibrationId, CancellationToken ct)
        {
            var calibration = await _db.MotorCalibrations
                .FirstOrDefaultAsync(m => m.MotorCalibrationId == motorCalibrationId, ct);
            if (calibration == null)
                throw new KeyNotFoundException($"No existe la caracterización {motorCalibrationId}.");

            // Solo una vigente por grupo: aceptar una desmarca las demás.
            var siblings = await _db.MotorCalibrations
                .Where(m => m.GroupCailbrationId == calibration.GroupCailbrationId)
                .ToListAsync(ct);

            foreach (var s in siblings) s.Acepted = 0;
            calibration.Acepted = 1;

            await _db.SaveChangesAsync(ct);
        }

        public async Task<MotorCalibrationDTO> CopyToGroupAsync(
            string sourceMotorCalibrationId, string targetGroupCalibrationId, CancellationToken ct)
        {
            var source = await Query().FirstOrDefaultAsync(m => m.MotorCalibrationId == sourceMotorCalibrationId, ct);
            if (source == null)
                throw new KeyNotFoundException($"No existe la caracterización {sourceMotorCalibrationId}.");

            var targetExists = await _db.GroupCalibrations
                .AnyAsync(g => g.GroupCailbrationId == targetGroupCalibrationId, ct);
            if (!targetExists)
                throw new KeyNotFoundException($"No existe el grupo de calibración {targetGroupCalibrationId}.");

            var copy = new MotorCalibration
            {
                MotorCalibrationId = Guid.NewGuid().ToString(),
                GroupCailbrationId = targetGroupCalibrationId,
                KimDeviceId = source.KimDeviceId,
                StepRate = source.StepRate,
                StepAcceleration = source.StepAcceleration,
                Status = source.Status,
                Acepted = 0,
                Date = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                AditionalInfo = $"Copiada del grupo {source.GroupCailbrationId}. {source.AditionalInfo}".Trim()
            };

            foreach (var axis in source.AxisStepCalibrations)
            {
                var axisCopy = new AxisStepCalibration
                {
                    AxisStepCalibrationId = Guid.NewGuid().ToString(),
                    MotorCalibrationId = copy.MotorCalibrationId,
                    AxisName = axis.AxisName,
                    StepSizeNmForward = axis.StepSizeNmForward,
                    StepSizeNmBackward = axis.StepSizeNmBackward,
                    StepSizeNm = axis.StepSizeNm,
                    HysteresisPct = axis.HysteresisPct,
                    RelativeErrorPct = axis.RelativeErrorPct,
                    Status = axis.Status
                };

                foreach (var m in axis.AxisStepMeasurements)
                {
                    axisCopy.AxisStepMeasurements.Add(new AxisStepMeasurement
                    {
                        AxisStepMeasurementId = Guid.NewGuid().ToString(),
                        AxisStepCalibrationId = axisCopy.AxisStepCalibrationId,
                        Direction = m.Direction,
                        Sequence = m.Sequence,
                        StepsCommanded = m.StepsCommanded,
                        CaliperReadingMm = m.CaliperReadingMm,
                        MeasuredAt = m.MeasuredAt
                    });
                }

                copy.AxisStepCalibrations.Add(axisCopy);
            }

            _db.MotorCalibrations.Add(copy);
            await _db.SaveChangesAsync(ct);

            return ToDto(copy);
        }

        public async Task DeleteAsync(string motorCalibrationId, CancellationToken ct)
        {
            var entity = await Query().FirstOrDefaultAsync(m => m.MotorCalibrationId == motorCalibrationId, ct);
            if (entity == null)
                throw new KeyNotFoundException($"No existe la caracterización {motorCalibrationId}.");

            _db.MotorCalibrations.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }

        // ── Interno ──────────────────────────────────────────────────────

        private IQueryable<MotorCalibration> Query() =>
            _db.MotorCalibrations
                .Include(m => m.AxisStepCalibrations)
                    .ThenInclude(a => a.AxisStepMeasurements);

        private async Task<AxisStepCalibration> LoadAxisAsync(string motorCalibrationId, string axisName, CancellationToken ct)
        {
            var normalized = NormalizeAxis(axisName);
            var axis = await _db.AxisStepCalibrations
                .Include(a => a.AxisStepMeasurements)
                .FirstOrDefaultAsync(a => a.MotorCalibrationId == motorCalibrationId && a.AxisName == normalized, ct);

            if (axis == null)
                throw new KeyNotFoundException($"La caracterización {motorCalibrationId} no tiene el eje '{normalized}'.");

            return axis;
        }

        private async Task<AxisStepCalibration> LoadAxisByIdAsync(string axisId, CancellationToken ct)
        {
            var axis = await _db.AxisStepCalibrations
                .Include(a => a.AxisStepMeasurements)
                .FirstOrDefaultAsync(a => a.AxisStepCalibrationId == axisId, ct);

            if (axis == null)
                throw new KeyNotFoundException($"No existe el eje {axisId}.");

            return axis;
        }

        private static void Recalculate(AxisStepCalibration axis)
        {
            var forward = StepSizeCalculator.FitTraverse(
                axis.AxisStepMeasurements.Where(m => m.Direction == "forward"));
            var backward = StepSizeCalculator.FitTraverse(
                axis.AxisStepMeasurements.Where(m => m.Direction == "backward"));

            StepSizeCalculator.Combine(axis, forward, backward);
        }

        /// <summary>
        /// La caracterización está completa cuando ambos ejes lo están. Se recalcula al
        /// vuelo en vez de exigir que el operador la cierre a mano.
        /// </summary>
        private async Task RefreshParentStatusAsync(string motorCalibrationId, CancellationToken ct)
        {
            var parent = await _db.MotorCalibrations
                .Include(m => m.AxisStepCalibrations)
                .FirstOrDefaultAsync(m => m.MotorCalibrationId == motorCalibrationId, ct);
            if (parent == null) return;

            var complete = parent.AxisStepCalibrations.Count > 0
                && parent.AxisStepCalibrations.All(a => a.Status == "complete");

            var newStatus = complete ? "complete" : "in_progress";
            if (parent.Status != newStatus)
            {
                parent.Status = newStatus;
                await _db.SaveChangesAsync(ct);
            }
        }

        private static void MoveAndWait(KCubeInertialMotor device, InertialMotorStatus.MotorChannels channel, int position, CancellationToken ct)
        {
            if (device.GetPosition(channel) == position) return;

            device.MoveTo(channel, position, 0);

            const int pollIntervalMs = 100;
            const int settleMs = 500;
            // Un tramo de la medición son decenas de miles de pasos: a 200 pasos/s eso son
            // varios minutos, muy por encima de la válvula de 2 min que usa el recorrido
            // normal. El límite aquí se dimensiona para el procedimiento, no se elimina.
            const int safetyMaxMs = 900_000; // 15 min

            var elapsed = 0;
            while (device.GetPosition(channel) != position)
            {
                ct.ThrowIfCancellationRequested();
                Thread.Sleep(pollIntervalMs);
                elapsed += pollIntervalMs;
                if (elapsed >= safetyMaxMs)
                    throw new InvalidOperationException(
                        $"El motor no alcanzó la posición {position} tras {safetyMaxMs / 60000} minutos. " +
                        "Verifique que el eje no esté trabado o al final de su recorrido.");
            }

            Thread.Sleep(settleMs);
        }

        private static InertialMotorStatus.MotorChannels ResolveChannel(string axisName) =>
            NormalizeAxis(axisName) == "x"
                ? InertialMotorStatus.MotorChannels.Channel1
                : InertialMotorStatus.MotorChannels.Channel2;

        private static string NormalizeAxis(string? axisName)
        {
            var value = (axisName ?? string.Empty).Trim().ToLowerInvariant();
            if (value != "x" && value != "y")
                throw new ArgumentException($"Eje no válido: '{axisName}'. Use 'x' o 'y'.");
            return value;
        }

        private static string NormalizeDirection(string? direction)
        {
            var value = (direction ?? string.Empty).Trim().ToLowerInvariant();
            if (value != "forward" && value != "backward")
                throw new ArgumentException($"Sentido no válido: '{direction}'. Use 'forward' o 'backward'.");
            return value;
        }

        private static MotorCalibrationDTO ToDto(MotorCalibration entity) => new()
        {
            MotorCalibrationId = entity.MotorCalibrationId,
            GroupCailbrationId = entity.GroupCailbrationId,
            KimDeviceId = entity.KimDeviceId,
            StepRate = entity.StepRate,
            StepAcceleration = entity.StepAcceleration,
            Status = entity.Status,
            Acepted = entity.Acepted,
            Date = entity.Date,
            AditionalInfo = entity.AditionalInfo,
            Axes = entity.AxisStepCalibrations
                .OrderBy(a => a.AxisName, StringComparer.Ordinal)
                .Select(ToDto)
                .ToList()
        };

        private static AxisStepCalibrationDTO ToDto(AxisStepCalibration axis)
        {
            var hysteresis = StepSizeCalculator.TryParse(axis.HysteresisPct, out var h) ? h : 0d;

            return new AxisStepCalibrationDTO
            {
                AxisStepCalibrationId = axis.AxisStepCalibrationId,
                AxisName = axis.AxisName,
                StepSizeNmForward = axis.StepSizeNmForward,
                StepSizeNmBackward = axis.StepSizeNmBackward,
                StepSizeNm = axis.StepSizeNm,
                HysteresisPct = axis.HysteresisPct,
                RelativeErrorPct = axis.RelativeErrorPct,
                Status = axis.Status,
                HysteresisWarning = axis.HysteresisPct != null && hysteresis > HysteresisWarningPct,
                Measurements = BuildMeasurementDtos(axis)
            };
        }

        private static List<AxisStepMeasurementDTO> BuildMeasurementDtos(AxisStepCalibration axis)
        {
            var result = new List<AxisStepMeasurementDTO>();

            foreach (var group in axis.AxisStepMeasurements.GroupBy(m => m.Direction))
            {
                var ordered = group.OrderBy(m => m.Sequence).ToList();
                var hasBaseline = ordered.Count > 0
                    && ordered[0].Sequence == 0
                    && StepSizeCalculator.TryParse(ordered[0].CaliperReadingMm, out _);

                double baseline = 0d;
                if (hasBaseline) StepSizeCalculator.TryParse(ordered[0].CaliperReadingMm, out baseline);

                foreach (var m in ordered)
                {
                    string? displacement = null;
                    if (hasBaseline && StepSizeCalculator.TryParse(m.CaliperReadingMm, out var reading))
                        displacement = StepSizeCalculator.Format(Math.Abs(reading - baseline));

                    result.Add(new AxisStepMeasurementDTO
                    {
                        AxisStepMeasurementId = m.AxisStepMeasurementId,
                        Direction = m.Direction,
                        Sequence = m.Sequence,
                        StepsCommanded = m.StepsCommanded,
                        CaliperReadingMm = m.CaliperReadingMm,
                        MeasuredAt = m.MeasuredAt,
                        DisplacementMm = displacement
                    });
                }
            }

            return result
                .OrderBy(m => m.Direction, StringComparer.Ordinal)
                .ThenBy(m => m.Sequence)
                .ToList();
        }
    }
}
