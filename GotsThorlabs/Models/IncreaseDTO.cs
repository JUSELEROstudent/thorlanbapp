using System.Text.Json.Serialization;

namespace GotsThorlabs.Models
{
    public class IncreaseDTO
    {
        public string IncreaseId { get; set; } = default!;
        public string Name { get; set; } = default!;

        // El frontend envía este campo como número JSON (input numérico con
        // v-model.number), pero se guarda como string en la base de datos.
        // Este converter acepta tanto string como number sin requerir cambios
        // en el frontend ni migración de base de datos.
        [JsonConverter(typeof(LenientStringJsonConverter))]
        public string Value { get; set; } = default!;

        public string AditionalInfo { get; set; } = default!;
    }
}
