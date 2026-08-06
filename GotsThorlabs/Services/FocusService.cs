using System.Globalization;
using GotsThorlabs.BLL;
using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Database.EntityRepo.Entities;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using Microsoft.EntityFrameworkCore;

namespace GotsThorlabs.Services
{
    public class FocusService : IFocusService
    {
        /// <summary>Capturas por defecto al aprender el umbral.</summary>
        private const int DefaultSamples = 5;

        /// <summary>Fracción de la nitidez medida que se guarda como umbral.</summary>
        private const double DefaultMargin = 0.7;

        /// <summary>Pausa entre capturas, para no medir el mismo cuadro varias veces.</summary>
        private const int SampleDelayMs = 120;

        private readonly ThorlabsDbContext _db;
        private readonly CameraServiceFactory _cameraFactory;

        public FocusService(ThorlabsDbContext db, CameraServiceFactory cameraFactory)
        {
            _db = db;
            _cameraFactory = cameraFactory;
        }

        public async Task<FocusEvaluationDTO> EvaluateAsync(string cameraName, CancellationToken ct)
        {
            var cameraRecord = await ResolveCameraAsync(cameraName, track: false, ct);
            var cameraService = _cameraFactory.GetService(cameraRecord.DriverType);

            ApplySettings(cameraService, cameraRecord);

            var score = CaptureSharpness(cameraService, cameraRecord);

            var criterion = FocusCriterion.FromSettings(cameraRecord.SettingsJson);
            var status = criterion.Evaluate(score);

            return new FocusEvaluationDTO
            {
                Score = Math.Round(score, 2),
                Threshold = criterion.Threshold,
                Status = status,
                IsAcceptable = status == FocusStatus.Acceptable,
                CameraName = cameraRecord.Name,
                CapturedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                Message = criterion.DescribeFailure(score)
            };
        }

        public async Task<FocusThresholdLearnedDTO> LearnThresholdAsync(
            string cameraName, int? samples, double? margin, CancellationToken ct)
        {
            // 3 capturas es el mínimo para que la mediana descarte un valor atípico;
            // por encima de 15 solo se alarga la espera del operador sin ganar precisión.
            var sampleCount = Math.Clamp(samples ?? DefaultSamples, 3, 15);

            // El margen no puede llegar a 1: el umbral quedaría exactamente en la nitidez
            // del momento de calibrar y cualquier variación normal lo haría fallar.
            var marginFraction = Math.Clamp(margin ?? DefaultMargin, 0.3, 0.95);

            var cameraRecord = await ResolveCameraAsync(cameraName, track: true, ct);
            var cameraService = _cameraFactory.GetService(cameraRecord.DriverType);

            ApplySettings(cameraService, cameraRecord);

            // El primer cuadro tras aplicar la configuración se descarta: en las cámaras
            // con exposición automática todavía viene del ajuste anterior y mediría una
            // nitidez que no corresponde a la configuración con la que se va a trabajar.
            try
            {
                CaptureSharpness(cameraService, cameraRecord);
            }
            catch (InvalidOperationException)
            {
                // Si ni el cuadro de descarte llega, la captura real dará el mismo error
                // con el mensaje que corresponde.
            }

            var readings = new List<double>(sampleCount);
            for (var i = 0; i < sampleCount; i++)
            {
                ct.ThrowIfCancellationRequested();
                readings.Add(CaptureSharpness(cameraService, cameraRecord));

                if (i < sampleCount - 1)
                    await Task.Delay(SampleDelayMs, ct);
            }

            var measured = Median(readings);
            var threshold = measured * marginFraction;

            var settings = CameraSettings.Parse(cameraRecord.SettingsJson) ?? new CameraSettings();
            settings.DriverType = cameraRecord.DriverType;
            settings.FocusThreshold = CameraSettings.Format(threshold);
            settings.FocusMetricVersion = FocusMetrics.MetricVersion;

            // Queda registrado con qué exposición y ganancia se midió: si luego cambian,
            // el umbral se marca solo como no calibrado en lugar de aprobar imágenes que
            // superan el umbral únicamente por el ruido de la nueva configuración.
            settings.FocusSettingsFingerprint = settings.Fingerprint();
            settings.UpdatedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

            cameraRecord.SettingsJson = settings.ToJson();
            await _db.SaveChangesAsync(ct);

            return new FocusThresholdLearnedDTO
            {
                CameraName = cameraRecord.Name,
                Samples = readings.Count,
                MeasuredSharpness = Math.Round(measured, 2),
                Margin = marginFraction,
                Threshold = Math.Round(threshold, 2),
                Readings = readings.Select(r => Math.Round(r, 2)).ToList(),
                Message = DescribeSpread(readings)
            };
        }

        private async Task<Camera> ResolveCameraAsync(string cameraName, bool track, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(cameraName))
                throw new ArgumentException("Debe indicar el nombre de la cámara.", nameof(cameraName));

            var normalized = cameraName.Trim().ToLower();

            var query = track ? _db.Cameras : _db.Cameras.AsNoTracking();
            var cameraRecord = await query.FirstOrDefaultAsync(c => c.Name.ToLower() == normalized, ct);

            if (cameraRecord == null)
                throw new KeyNotFoundException($"No existe una cámara registrada con el nombre '{cameraName}'.");

            return cameraRecord;
        }

        /// <summary>
        /// Se mide con la MISMA configuración con la que se va a calibrar: medir el
        /// enfoque con otros parámetros de exposición daría un número que no corresponde
        /// a las imágenes que realmente se van a capturar.
        /// </summary>
        private static void ApplySettings(ICameraService cameraService, Camera cameraRecord)
        {
            try
            {
                cameraService.ApplySettings(cameraRecord.LocalIdentifier, cameraRecord.SettingsJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FocusService] No se pudo aplicar la configuración de la cámara: {ex.Message}");
            }
        }

        private static double CaptureSharpness(ICameraService cameraService, Camera cameraRecord)
        {
            using var frame = cameraService.CaptureFrame(cameraRecord.LocalIdentifier);

            if (frame == null || frame.Empty())
            {
                throw new InvalidOperationException(
                    $"La cámara '{cameraRecord.Name}' no devolvió imagen. Verifique que esté conectada y que " +
                    "ninguna otra aplicación (por ejemplo IDS Cockpit) la esté usando.");
            }

            return FocusMetrics.Sharpness(frame);
        }

        private static double Median(List<double> values)
        {
            var ordered = values.OrderBy(v => v).ToList();
            var mid = ordered.Count / 2;

            return ordered.Count % 2 == 1
                ? ordered[mid]
                : (ordered[mid - 1] + ordered[mid]) / 2d;
        }

        /// <summary>
        /// Avisa cuando las capturas no coinciden entre sí. Una dispersión alta con la
        /// muestra quieta significa que algo está variando entre cuadros —vibración,
        /// exposición automática buscando, iluminación inestable— y el umbral aprendido
        /// heredaría ese ruido.
        /// </summary>
        private static string? DescribeSpread(List<double> readings)
        {
            if (readings.Count < 2) return null;

            var min = readings.Min();
            var max = readings.Max();

            if (min <= 0d || max / min < 1.5) return null;

            return $"Las capturas variaron bastante entre sí (de {min:F1} a {max:F1}) con la muestra quieta. " +
                   "Puede deberse a vibración, exposición automática o iluminación inestable. " +
                   "Conviene estabilizar el montaje y repetir la calibración del umbral.";
        }
    }
}
