using AForge.Video.DirectShow;
using OpenCvSharp;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;

namespace GotsThorlabs.Services
{
    public class VideoCaptureCameraService : ICameraService, ICameraDiscoveryService, ICameraParameterProvider, IDisposable
    {
        private readonly Dictionary<int, SemaphoreSlim> _deviceLocks = new();
        private readonly object _dictLock = new();

        // Configuración vigente por dispositivo. DirectShow abre y cierra en cada
        // captura, así que no hay sesión que invalidar: basta con re-aplicar los
        // valores cada vez que se abre el dispositivo.
        private readonly Dictionary<string, CameraSettings?> _settings = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _settingsLock = new();

        private bool _disposed = false;

        // Parámetros soportados. OpenCV/DirectShow no permite consultar rangos al
        // dispositivo (Min/Max quedan en null y el frontend muestra un campo libre),
        // y qué parámetros respeta realmente depende del driver de cada webcam:
        // escribir uno no soportado simplemente no tiene efecto, no falla.
        private static readonly CameraParameterDescriptor[] Catalog =
        {
            new() { Name = "FrameWidth",  Label = "Ancho de imagen",    Type = "number", Unit = "px",  Group = "Resolución",
                    Description = "Ancho solicitado al dispositivo. La cámara puede ajustarlo al valor soportado más cercano." },
            new() { Name = "FrameHeight", Label = "Alto de imagen",     Type = "number", Unit = "px",  Group = "Resolución",
                    Description = "Alto solicitado al dispositivo." },
            new() { Name = "Fps",         Label = "Cuadros por segundo", Type = "number", Unit = "fps", Group = "Temporización" },
            new() { Name = "Exposure",    Label = "Exposición",          Type = "number", Group = "Exposición",
                    Description = "En DirectShow suele ser una escala logarítmica con valores negativos (por ejemplo -6). Ajústelo observando el streaming." },
            new() { Name = "Gain",        Label = "Ganancia",            Type = "number", Group = "Exposición",
                    Description = "Amplifica la señal del sensor. Subirla aclara la imagen pero agrega ruido; prefiera ajustar la exposición primero." },
            new() { Name = "Brightness",  Label = "Brillo",              Type = "number", Group = "Imagen" },
            new() { Name = "Contrast",    Label = "Contraste",           Type = "number", Group = "Imagen" },
            new() { Name = "Saturation",  Label = "Saturación",          Type = "number", Group = "Color" },
            new() { Name = "Hue",         Label = "Tono",                Type = "number", Group = "Color" },
            new() { Name = "AutoFocus",   Label = "Enfoque automático",  Type = "bool",   Group = "Enfoque",
                    Description = "Desactívelo para calibrar: si la cámara reenfoca entre capturas, el desplazamiento medido queda contaminado." },
        };

        public Mat CaptureFrame(int cameraId)
        {
            var sem = GetOrCreateLock(cameraId);
            sem.Wait();
            try
            {
                using var capture = new VideoCapture(cameraId, VideoCaptureAPIs.DSHOW);
                var frame = new Mat();

                // El constructor ya abre el dispositivo. Antes la configuración se
                // aplicaba dentro de un "if (!capture.IsOpened())", es decir, solo
                // cuando la cámara NO había abierto: nunca se ejecutaba en el caso
                // normal. Ahora, si no abre se sale de inmediato y, si abre, se aplica
                // la configuración antes de leer el cuadro.
                if (!capture.IsOpened())
                {
                    Console.WriteLine($"[VideoCapture] No se pudo abrir la cámara {cameraId}.");
                    return frame;
                }

                ApplyToCapture(capture, cameraId.ToString());

                capture.Read(frame);
                return frame;
            }
            finally
            {
                sem.Release();
            }
        }

        public Mat CaptureFrame(string localIdentifier)
        {
            if (!int.TryParse(localIdentifier, out var cameraId))
                return new Mat();

            return CaptureFrame(cameraId);
        }

        // DirectShow opens/closes per capture, so there is no persistent
        // connection to release. No-op to satisfy the interface contract.
        public void ReleaseConnection() { }

        public void ApplySettings(string localIdentifier, string? settingsJson)
        {
            if (string.IsNullOrWhiteSpace(localIdentifier)) return;

            var parsed = CameraSettings.Parse(settingsJson);
            lock (_settingsLock)
            {
                _settings[localIdentifier.Trim()] = parsed;
            }
        }

        public IReadOnlyList<CameraParameterDescriptor> GetParameters(string localIdentifier)
        {
            // Copia del catálogo: nunca se devuelven las instancias estáticas, porque
            // se les asigna el valor actual y son compartidas entre peticiones.
            var descriptors = Catalog.Select(Clone).ToList();

            if (!int.TryParse(localIdentifier, out var cameraId))
                return descriptors;

            var sem = GetOrCreateLock(cameraId);
            sem.Wait();
            try
            {
                using var capture = new VideoCapture(cameraId, VideoCaptureAPIs.DSHOW);
                if (!capture.IsOpened())
                {
                    Console.WriteLine($"[VideoCapture] GetParameters: la cámara {cameraId} no está disponible; se devuelve el catálogo sin valores.");
                    return descriptors;
                }

                foreach (var d in descriptors)
                {
                    try
                    {
                        var value = ReadProperty(capture, d.Name);
                        if (value.HasValue)
                        {
                            d.Current = d.Type == "bool"
                                ? (Math.Abs(value.Value) > double.Epsilon ? "true" : "false")
                                : CameraSettings.Format(value.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[VideoCapture] No se pudo leer '{d.Name}': {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VideoCapture] GetParameters falló para la cámara {cameraId}: {ex.Message}");
            }
            finally
            {
                sem.Release();
            }

            return descriptors;
        }

        public IEnumerable<DeviceInfo> EnumerateDevices()
        {
            var result = new List<DeviceInfo>();
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            for (int i = 0; i < videoDevices.Count; i++)
            {
                var d = videoDevices[i];
                result.Add(new DeviceInfo
                {
                    Index = i,
                    // DirectShow devices have no serial number; use the index as stable identifier.
                    SerialNumber = i.ToString(),
                    DisplayName = d.Name ?? string.Empty,
                    MonikerString = d.MonikerString ?? string.Empty
                });
            }
            return result;
        }

        // ── Configuración ────────────────────────────────────────────────

        private void ApplyToCapture(VideoCapture capture, string key)
        {
            CameraSettings? settings;
            lock (_settingsLock)
            {
                _settings.TryGetValue(key, out settings);
            }

            if (settings == null || settings.Values == null || settings.Values.Count == 0)
                return;

            foreach (var descriptor in Catalog)
            {
                if (!settings.Has(descriptor.Name)) continue;

                try
                {
                    if (descriptor.Type == "bool")
                    {
                        if (settings.TryGetBool(descriptor.Name, out var flag))
                            WriteProperty(capture, descriptor.Name, flag ? 1d : 0d);
                    }
                    else if (settings.TryGetDouble(descriptor.Name, out var numeric))
                    {
                        WriteProperty(capture, descriptor.Name, numeric);
                    }
                }
                catch (Exception ex)
                {
                    // Un parámetro que la webcam no soporta no debe impedir capturar.
                    Console.WriteLine($"[VideoCapture] No se pudo aplicar '{descriptor.Name}': {ex.Message}");
                }
            }
        }

        // Se usan las propiedades de conveniencia de OpenCvSharp en vez de
        // Set(VideoCaptureProperties, double) para no depender del nombre del enum,
        // que ha cambiado entre versiones de la librería.
        private static void WriteProperty(VideoCapture capture, string name, double value)
        {
            switch (name)
            {
                case "FrameWidth":  capture.FrameWidth  = (int)Math.Round(value); break;
                case "FrameHeight": capture.FrameHeight = (int)Math.Round(value); break;
                case "Fps":         capture.Fps         = value; break;
                case "Exposure":    capture.Exposure    = value; break;
                case "Gain":        capture.Gain        = value; break;
                case "Brightness":  capture.Brightness  = value; break;
                case "Contrast":    capture.Contrast    = value; break;
                case "Saturation":  capture.Saturation  = value; break;
                case "Hue":         capture.Hue         = value; break;
                case "AutoFocus":   capture.AutoFocus   = Math.Abs(value) > double.Epsilon; break;
            }
        }

        private static double? ReadProperty(VideoCapture capture, string name) => name switch
        {
            "FrameWidth"  => capture.FrameWidth,
            "FrameHeight" => capture.FrameHeight,
            "Fps"         => capture.Fps,
            "Exposure"    => capture.Exposure,
            "Gain"        => capture.Gain,
            "Brightness"  => capture.Brightness,
            "Contrast"    => capture.Contrast,
            "Saturation"  => capture.Saturation,
            "Hue"         => capture.Hue,
            "AutoFocus"   => capture.AutoFocus ? 1d : 0d,
            _             => null
        };

        private static CameraParameterDescriptor Clone(CameraParameterDescriptor source) => new()
        {
            Name = source.Name,
            Label = source.Label,
            Type = source.Type,
            Min = source.Min,
            Max = source.Max,
            Step = source.Step,
            Unit = source.Unit,
            Options = source.Options,
            Group = source.Group,
            Description = source.Description
        };

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

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var sem in _deviceLocks.Values)
                sem.Dispose();
        }
    }
}
