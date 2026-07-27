namespace GotsThorlabs.Models
{
    public class CameraDTO
    {
        public string CameraId { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string LocalIdentifier { get; set; } = default!;
        public string Features { get; set; } = default!;
        public string DriverType { get; set; } = "generic";

        /// <summary>
        /// Parámetros de captura serializados como JSON. Opcional: si viene nulo en un
        /// update, la configuración existente se conserva (ver CameraCrudService).
        /// Normalmente no se envía desde el formulario de la cámara, sino desde la vista
        /// de parámetros (PUT /api/camera/{id}/parameters).
        /// </summary>
        public string? SettingsJson { get; set; }
    }
}
