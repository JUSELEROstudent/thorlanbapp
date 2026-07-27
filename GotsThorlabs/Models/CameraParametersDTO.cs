using System.Globalization;
using System.Text.Json;

namespace GotsThorlabs.Models
{
    /// <summary>Respuesta de GET /api/camera/{id}/parameters.</summary>
    public class CameraParametersResponseDTO
    {
        public string CameraId { get; set; } = default!;
        public string CameraName { get; set; } = default!;
        public string DriverType { get; set; } = default!;

        /// <summary>
        /// False cuando el driver de esta cámara todavía no soporta configuración de
        /// parámetros (hoy, ids_peak_dotnet). El frontend muestra el aviso de Message
        /// en lugar del formulario.
        /// </summary>
        public bool SupportsParameters { get; set; }

        public string? Message { get; set; }

        /// <summary>Lo que el dispositivo acepta, con sus rangos y valores actuales.</summary>
        public List<CameraParameterDescriptor> Descriptors { get; set; } = new();

        /// <summary>Lo que el usuario dejó guardado para esta cámara.</summary>
        public Dictionary<string, string> Saved { get; set; } = new();

        public string? FocusThreshold { get; set; }
    }

    /// <summary>Cuerpo de PUT /api/camera/{id}/parameters.</summary>
    public class CameraParametersUpdateDTO
    {
        /// <summary>
        /// Valores a guardar. Se reciben como JsonElement y no como string para
        /// aceptar indistintamente números, booleanos o texto: el frontend envía
        /// números desde los campos numéricos y booleanos desde los interruptores,
        /// y exigir comillas rompería la petición con un 400 antes de llegar aquí
        /// (el mismo motivo por el que existe LenientStringJsonConverter).
        /// </summary>
        public Dictionary<string, JsonElement>? Values { get; set; }

        public JsonElement? FocusThreshold { get; set; }

        /// <summary>
        /// Normaliza los valores recibidos a texto invariante, que es como se
        /// persisten en settingsJson. Las claves sin valor se descartan.
        /// </summary>
        public Dictionary<string, string> NormalizeValues()
        {
            var result = new Dictionary<string, string>();
            if (Values == null) return result;

            foreach (var (key, element) in Values)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;

                var text = ToInvariantString(element);
                if (!string.IsNullOrWhiteSpace(text))
                    result[key.Trim()] = text!;
            }
            return result;
        }

        public string? NormalizeFocusThreshold() =>
            FocusThreshold.HasValue ? ToInvariantString(FocusThreshold.Value) : null;

        private static string? ToInvariantString(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetDouble(out var d)
                                        ? d.ToString("G", CultureInfo.InvariantCulture)
                                        : element.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => element.ToString()
        };
    }
}
