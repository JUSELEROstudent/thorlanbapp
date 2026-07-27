namespace GotsThorlabs.Models
{
    /// <summary>
    /// Un cuadro del streaming acompañado de su medida de enfoque.
    /// Lo emite StreamingHub.CounterWithMetrics.
    ///
    /// SignalR serializa Frame (byte[]) como base64, igual que el stream Counter
    /// original, así que el cliente lo sigue usando como "data:image/png;base64,...".
    /// </summary>
    public class StreamFrameDTO
    {
        /// <summary>Imagen codificada. Llega al cliente como base64.</summary>
        public byte[] Frame { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Varianza del Laplaciano del cuadro. A mayor valor, más nítida la imagen.
        /// No tiene escala absoluta: solo es comparable entre cuadros del mismo montaje.
        /// </summary>
        public double Focus { get; set; }

        /// <summary>Umbral configurado para esta cámara, o null si no se ha definido.</summary>
        public double? FocusThreshold { get; set; }

        /// <summary>
        /// True cuando no hay umbral definido o el enfoque lo alcanza. Se calcula en el
        /// servidor para que el cliente no tenga que replicar el criterio.
        /// </summary>
        public bool IsFocusAcceptable { get; set; } = true;
    }

    /// <summary>Respuesta de POST /api/Focus/evaluate.</summary>
    public class FocusEvaluationDTO
    {
        public double Score { get; set; }
        public double? Threshold { get; set; }
        public bool IsAcceptable { get; set; } = true;
        public string? CameraName { get; set; }
        public string CapturedAt { get; set; } = default!;

        /// <summary>Mensaje listo para mostrar cuando el enfoque es insuficiente.</summary>
        public string? Message { get; set; }
    }
}
