namespace GotsThorlabs.Models
{
    public class GroupCalibrationDTO
    {
        public string GroupCailbrationId { get; set; } = default!;
        public string CameraId { get; set; } = default!;
        public string MicroscopeId { get; set; } = default!;
        public string IncreaseId { get; set; } = default!;
        public string Date { get; set; } = default!;
        public string AditionalInfo { get; set; } = default!;
    }

    /// <summary>
    /// Un grupo de calibración con el NOMBRE de los equipos que lo componen, no solo
    /// sus identificadores.
    ///
    /// Antes GET /api/GroupCalibration devolvía la entidad sin cargar sus relaciones,
    /// así que camera/microscope/increase llegaban en null y la vista tenía que
    /// resolver cada nombre buscando el id en listas que pedía por separado. Eso deja
    /// la pantalla mostrando "no encontrado" cuando esas listas aún no cargaron o
    /// cuando un id quedó huérfano, aunque el dato exista en la base.
    ///
    /// Se aplanan solo los nombres en vez de incluir las entidades completas: es lo
    /// único que la tarjeta necesita, y evita arrastrar las colecciones anidadas de
    /// cada equipo en cada elemento de la lista.
    /// </summary>
    public class GroupCalibrationResponseDTO
    {
        public string GroupCailbrationId { get; set; } = default!;
        public string CameraId { get; set; } = default!;
        public string MicroscopeId { get; set; } = default!;
        public string IncreaseId { get; set; } = default!;
        public string Date { get; set; } = default!;
        public string AditionalInfo { get; set; } = default!;

        /// <summary>Nombre de la cámara, o null si el id no corresponde a ninguna.</summary>
        public string? CameraName { get; set; }

        /// <summary>Nombre del microscopio, o null si el id no corresponde a ninguno.</summary>
        public string? MicroscopeName { get; set; }

        /// <summary>Nombre del objetivo/aumento, o null si el id no corresponde a ninguno.</summary>
        public string? IncreaseName { get; set; }

        /// <summary>Aumento (p. ej. "40x"); acompaña a IncreaseName al mostrarlo.</summary>
        public string? IncreaseValue { get; set; }
    }
}
