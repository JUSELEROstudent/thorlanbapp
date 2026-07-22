using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GotsThorlabs.Models
{
    /// <summary>
    /// Convierte valores JSON tipo string O number hacia una propiedad string de C#.
    /// El frontend (Nuxt) a veces envía campos "numéricos" (ej. Increase.Value) como
    /// número JSON crudo (por usar v-model.number en el input), y por defecto
    /// System.Text.Json rechaza eso con un JsonException, lo que bajo [ApiController]
    /// se convierte automáticamente en un 400 Bad Request antes de que el controller
    /// llegue a ejecutarse. Este converter acepta ambos formatos (string o number) y
    /// siempre entrega/serializa como string, sin necesidad de migrar la base de datos
    /// ni de tocar el input del frontend.
    /// </summary>
    public class LenientStringJsonConverter : JsonConverter<string?>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return reader.GetString();

                case JsonTokenType.Number:
                    // Se preserva el número tal cual vino (sin perder precisión ni
                    // agregar decimales de más, ej. 5 -> "5", no "5.0").
                    if (reader.TryGetInt64(out var longValue))
                        return longValue.ToString(CultureInfo.InvariantCulture);
                    if (reader.TryGetDecimal(out var decValue))
                        return decValue.ToString(CultureInfo.InvariantCulture);
                    return reader.GetDouble().ToString(CultureInfo.InvariantCulture);

                case JsonTokenType.Null:
                    return null;

                default:
                    throw new JsonException($"No se pudo convertir el token '{reader.TokenType}' a string.");
            }
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            if (value is null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(value);
        }
    }
}
