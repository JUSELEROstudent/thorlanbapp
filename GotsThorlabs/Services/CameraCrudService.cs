using GotsThorlabs.BLL;
using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Database.EntityRepo.Entities;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using Microsoft.EntityFrameworkCore;

namespace GotsThorlabs.Services
{
    public class CameraCrudService : ICameraCrudService
    {
        private readonly ThorlabsDbContext _db;
        private readonly CameraServiceFactory _cameraFactory;

        public CameraCrudService(ThorlabsDbContext db, CameraServiceFactory cameraFactory)
        {
            _db = db;
            _cameraFactory = cameraFactory;
        }

        public async Task<IEnumerable<Camera>> GetAllAsync(CancellationToken ct)
        {
            return await _db.Cameras.AsNoTracking().ToListAsync(ct);
        }

        public async Task<Camera?> GetByIdAsync(string id, CancellationToken ct)
        {
            return await _db.Cameras.AsNoTracking().FirstOrDefaultAsync(c => c.CameraId == id, ct);
        }

        public async Task<Camera> CreateAsync(CameraDTO dto, CancellationToken ct)
        {
            var driverType = string.IsNullOrWhiteSpace(dto.DriverType) ? "generic" : dto.DriverType.Trim().ToLowerInvariant();

            var entity = new Camera
            {
                CameraId = string.IsNullOrWhiteSpace(dto.CameraId) ? Guid.NewGuid().ToString() : dto.CameraId,
                Name = dto.Name,
                LocalIdentifier = dto.LocalIdentifier,
                Features = dto.Features,
                DriverType = driverType
            };

            if (!string.IsNullOrWhiteSpace(entity.LocalIdentifier))
            {
                var resolved = _cameraFactory.ResolveLocalIdentifier(driverType, entity.LocalIdentifier);
                if (resolved is not null)
                    entity.LocalIdentifier = resolved;
            }

            var cameraExist = await _db.Cameras.AnyAsync(item => item.Name.Trim().ToLower() == entity.Name.Trim().ToLower(), ct);
            if (cameraExist)
                throw new InvalidOperationException("Ya existe una cámara con ese nombre.");

            _db.Cameras.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity;
        }

        public async Task UpdateAsync(string id, CameraDTO dto, CancellationToken ct)
        {
            var existing = await _db.Cameras.FirstOrDefaultAsync(c => c.CameraId == id, ct);
            if (existing is null) throw new KeyNotFoundException($"Camera {id} not found.");

            existing.Name = dto.Name;
            existing.LocalIdentifier = dto.LocalIdentifier;
            existing.Features = dto.Features;
            existing.DriverType = string.IsNullOrWhiteSpace(dto.DriverType) ? "generic" : dto.DriverType.Trim().ToLowerInvariant();

            // La configuración de parámetros solo se toca si viene explícitamente en el
            // DTO. El formulario de la cámara no la envía, y sobrescribirla con null
            // desde ahí borraría en silencio lo ajustado en la vista de parámetros.
            if (dto.SettingsJson != null)
                existing.SettingsJson = dto.SettingsJson;

            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(string id, CancellationToken ct)
        {
            var camera = await _db.Cameras.FirstOrDefaultAsync(c => c.CameraId == id, ct);
            if (camera is null) throw new KeyNotFoundException($"Camera {id} not found.");

            _db.Cameras.Remove(camera);
            await _db.SaveChangesAsync(ct);
        }

        // ── Parámetros de captura ────────────────────────────────────────

        public async Task<CameraParametersResponseDTO> GetParametersAsync(string id, CancellationToken ct)
        {
            var camera = await _db.Cameras.AsNoTracking().FirstOrDefaultAsync(c => c.CameraId == id, ct);
            if (camera is null) throw new KeyNotFoundException($"Camera {id} not found.");

            var saved = CameraSettings.Parse(camera.SettingsJson);

            var response = new CameraParametersResponseDTO
            {
                CameraId = camera.CameraId,
                CameraName = camera.Name,
                DriverType = camera.DriverType,
                Saved = saved?.Values ?? new Dictionary<string, string>(),
                FocusThreshold = saved?.FocusThreshold,
                IsFocusThresholdCalibrated = FocusCriterion.FromSettings(camera.SettingsJson).IsCalibrated
            };

            var provider = _cameraFactory.GetParameterProvider(camera.DriverType);
            if (provider is null)
            {
                response.SupportsParameters = false;
                response.Message = $"El driver '{camera.DriverType}' todavía no permite configurar parámetros " +
                                   "desde la aplicación. La cámara se usa con su configuración por defecto.";
                return response;
            }

            response.SupportsParameters = true;

            try
            {
                // Consultar al dispositivo puede tardar (abrir la cámara) o fallar si
                // está desconectada; eso no debe romper la vista, solo dejarla sin
                // rangos ni valores actuales.
                response.Descriptors = provider.GetParameters(camera.LocalIdentifier).ToList();

                if (response.Descriptors.Count == 0)
                {
                    response.Message = "No se pudo consultar el dispositivo. Verifique que la cámara esté " +
                                       "conectada y que ninguna otra aplicación la esté usando.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CameraCrud] GetParameters falló para '{camera.Name}': {ex.Message}");
                response.Message = $"No se pudo consultar el dispositivo: {ex.Message}";
            }

            return response;
        }

        public async Task UpdateParametersAsync(string id, CameraParametersUpdateDTO dto, CancellationToken ct)
        {
            var camera = await _db.Cameras.FirstOrDefaultAsync(c => c.CameraId == id, ct);
            if (camera is null) throw new KeyNotFoundException($"Camera {id} not found.");

            var values = dto.NormalizeValues();

            // Se validan las claves contra lo que el dispositivo declara soportar, pero
            // solo si se pudo consultar: si la cámara está desconectada, rechazar todo
            // impediría preparar la configuración antes de conectarla.
            var provider = _cameraFactory.GetParameterProvider(camera.DriverType);
            if (provider != null && values.Count > 0)
            {
                IReadOnlyList<CameraParameterDescriptor> descriptors;
                try
                {
                    descriptors = provider.GetParameters(camera.LocalIdentifier);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CameraCrud] No se pudieron validar los parámetros contra el dispositivo: {ex.Message}");
                    descriptors = Array.Empty<CameraParameterDescriptor>();
                }

                if (descriptors.Count > 0)
                {
                    var known = descriptors.Select(d => d.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var unknown = values.Keys.Where(k => !known.Contains(k)).ToList();
                    if (unknown.Count > 0)
                    {
                        throw new ArgumentException(
                            $"El dispositivo no acepta estos parámetros: {string.Join(", ", unknown)}.");
                    }
                }
            }

            var settings = CameraSettings.Parse(camera.SettingsJson) ?? new CameraSettings();
            settings.DriverType = camera.DriverType;
            settings.Values = values;
            settings.UpdatedAt = DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture);

            var threshold = dto.NormalizeFocusThreshold();
            if (threshold != null)
            {
                settings.FocusThreshold = threshold;

                // Un umbral escrito a mano se marca con la versión de métrica actual: sin
                // el sello quedaría como heredado y el veredicto de nitidez saldría siempre
                // como "sin calibrar", ignorando en silencio lo que el usuario acaba de
                // guardar. Lo recomendable sigue siendo aprenderlo con
                // POST /api/Focus/learn-threshold, que lo mide en lugar de estimarlo.
                settings.FocusMetricVersion = FocusMetrics.MetricVersion;
                settings.FocusSettingsFingerprint = settings.Fingerprint();
            }

            camera.SettingsJson = settings.ToJson();
            await _db.SaveChangesAsync(ct);

            // Se avisa al driver de inmediato. Importa sobre todo para la uEye, que
            // mantiene la cámara abierta entre capturas: sin esto seguiría usando los
            // valores anteriores hasta que algo más cerrara la sesión.
            try
            {
                _cameraFactory.GetService(camera.DriverType)
                    .ApplySettings(camera.LocalIdentifier, camera.SettingsJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CameraCrud] No se pudo notificar la configuración al driver: {ex.Message}");
            }
        }
    }
}
