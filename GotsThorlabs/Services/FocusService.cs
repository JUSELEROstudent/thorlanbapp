using System.Globalization;
using GotsThorlabs.BLL;
using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using Microsoft.EntityFrameworkCore;

namespace GotsThorlabs.Services
{
    public class FocusService : IFocusService
    {
        private readonly ThorlabsDbContext _db;
        private readonly CameraServiceFactory _cameraFactory;

        public FocusService(ThorlabsDbContext db, CameraServiceFactory cameraFactory)
        {
            _db = db;
            _cameraFactory = cameraFactory;
        }

        public async Task<FocusEvaluationDTO> EvaluateAsync(string cameraName, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(cameraName))
                throw new ArgumentException("Debe indicar el nombre de la cámara.", nameof(cameraName));

            var normalized = cameraName.Trim().ToLower();
            var cameraRecord = await _db.Cameras
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name.ToLower() == normalized, ct);

            if (cameraRecord == null)
                throw new KeyNotFoundException($"No existe una cámara registrada con el nombre '{cameraName}'.");

            var cameraService = _cameraFactory.GetService(cameraRecord.DriverType);

            // Se evalúa con la MISMA configuración con la que se va a calibrar: medir
            // el enfoque con otros parámetros de exposición daría un número que no
            // corresponde a las imágenes que realmente se van a capturar.
            try
            {
                cameraService.ApplySettings(cameraRecord.LocalIdentifier, cameraRecord.SettingsJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FocusService] No se pudo aplicar la configuración de la cámara: {ex.Message}");
            }

            double score;
            using (var frame = cameraService.CaptureFrame(cameraRecord.LocalIdentifier))
            {
                if (frame == null || frame.Empty())
                {
                    throw new InvalidOperationException(
                        $"La cámara '{cameraRecord.Name}' no devolvió imagen. Verifique que esté conectada y que " +
                        "ninguna otra aplicación (por ejemplo IDS Cockpit) la esté usando.");
                }

                score = FocusMetrics.VarianceOfLaplacian(frame);
            }

            var threshold = ParseThreshold(cameraRecord.SettingsJson);
            var isAcceptable = !threshold.HasValue || score >= threshold.Value;

            return new FocusEvaluationDTO
            {
                Score = Math.Round(score, 2),
                Threshold = threshold,
                IsAcceptable = isAcceptable,
                CameraName = cameraRecord.Name,
                CapturedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                Message = isAcceptable
                    ? null
                    : $"El enfoque medido ({score:F1}) está por debajo del umbral configurado para esta cámara " +
                      $"({threshold!.Value:F1}). Ajuste el enfoque del microscopio antes de calibrar: una muestra " +
                      "desenfocada hace que la correlación de fase mida desplazamientos poco confiables."
            };
        }

        private static double? ParseThreshold(string? settingsJson)
        {
            var settings = CameraSettings.Parse(settingsJson);
            if (settings == null || string.IsNullOrWhiteSpace(settings.FocusThreshold))
                return null;

            return double.TryParse(settings.FocusThreshold, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : null;
        }
    }
}
