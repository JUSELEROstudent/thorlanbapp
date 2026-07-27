using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GotsThorlabs.Models
{
    /// <summary>
    /// Contenido de la columna camera.settingsJson: los parámetros de captura
    /// configurados para una cámara concreta.
    ///
    /// Los valores se guardan como texto con cultura invariante (igual que Dx/Dy/
    /// Confidence en PicsCalibration) para no depender de la configuración regional
    /// del equipo del laboratorio: con coma decimal, un double serializado y vuelto
    /// a leer con otra cultura se corrompe silenciosamente.
    /// </summary>
    public class CameraSettings
    {
        /// <summary>
        /// Driver para el que se guardó esta configuración. Permite detectar que la
        /// cámara cambió de driver y que los parámetros ya no aplican.
        /// </summary>
        public string? DriverType { get; set; }

        public string? UpdatedAt { get; set; }

        /// <summary>
        /// Umbral de nitidez (varianza del Laplaciano) por debajo del cual se advierte
        /// al usuario antes de lanzar una calibración. Se guarda por cámara porque la
        /// escala depende del montaje óptico y no tiene un valor universal.
        /// </summary>
        public string? FocusThreshold { get; set; }

        public Dictionary<string, string> Values { get; set; } = new();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Deserializa el contenido de settingsJson. Devuelve null si viene vacío o
        /// malformado: una configuración corrupta no debe impedir capturar, solo
        /// hace que se use el comportamiento por defecto del driver.
        /// </summary>
        public static CameraSettings? Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                var parsed = JsonSerializer.Deserialize<CameraSettings>(json, JsonOptions);
                if (parsed == null) return null;
                parsed.Values ??= new Dictionary<string, string>();
                return parsed;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[CameraSettings] settingsJson inválido, se ignora: {ex.Message}");
                return null;
            }
        }

        public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

        public bool TryGetDouble(string name, out double value)
        {
            value = 0d;
            if (Values == null || !Values.TryGetValue(name, out var raw) || string.IsNullOrWhiteSpace(raw))
                return false;

            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        public bool TryGetInt(string name, out int value)
        {
            value = 0;
            if (!TryGetDouble(name, out var d)) return false;
            value = (int)Math.Round(d);
            return true;
        }

        /// <summary>
        /// Lee un booleano. Acepta true/false y también 1/0, porque el frontend puede
        /// mandar cualquiera de las dos formas según el control que se use.
        /// </summary>
        public bool TryGetBool(string name, out bool value)
        {
            value = false;
            if (Values == null || !Values.TryGetValue(name, out var raw) || string.IsNullOrWhiteSpace(raw))
                return false;

            raw = raw.Trim();
            if (bool.TryParse(raw, out value)) return true;

            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var numeric))
            {
                value = Math.Abs(numeric) > double.Epsilon;
                return true;
            }
            return false;
        }

        public string? GetString(string name)
        {
            if (Values == null || !Values.TryGetValue(name, out var raw) || string.IsNullOrWhiteSpace(raw))
                return null;
            return raw.Trim();
        }

        public bool Has(string name) =>
            Values != null && Values.TryGetValue(name, out var raw) && !string.IsNullOrWhiteSpace(raw);

        /// <summary>
        /// Huella del contenido efectivo. Los drivers con sesión persistente (uEye) la
        /// usan para saber si hay que cerrar y reabrir la cámara: si la configuración
        /// no cambió, reabrir costaría varios segundos sin ninguna ganancia.
        /// </summary>
        public string Fingerprint()
        {
            if (Values == null || Values.Count == 0)
                return string.Empty;

            var ordered = Values
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => $"{kv.Key}={kv.Value.Trim()}");

            return string.Join("|", ordered);
        }

        public static string FingerprintOf(string? settingsJson) =>
            Parse(settingsJson)?.Fingerprint() ?? string.Empty;

        /// <summary>Convierte un double a texto invariante, el formato en que se persiste.</summary>
        public static string Format(double value) =>
            value.ToString("G", CultureInfo.InvariantCulture);
    }
}
