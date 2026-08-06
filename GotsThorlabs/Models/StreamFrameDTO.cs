using System.Text.Json.Serialization;

namespace GotsThorlabs.Models
{
    /// <summary>
    /// Veredicto sobre la nitidez de un cuadro.
    ///
    /// Existe como tres estados y no como booleano porque "no se sabe" no es lo mismo
    /// que "está bien": antes, una cámara sin umbral configurado se reportaba como
    /// aceptable, de modo que el indicador salía en verde justo en el caso en el que
    /// no había ningún criterio con el que juzgar.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum FocusStatus
    {
        /// <summary>No hay umbral utilizable para esta cámara: no se puede opinar.</summary>
        Unknown,

        /// <summary>La nitidez medida está por debajo del umbral aprendido.</summary>
        Insufficient,

        /// <summary>La nitidez medida alcanza el umbral aprendido.</summary>
        Acceptable
    }

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
        /// Nitidez normalizada del cuadro (FocusMetrics.Sharpness). A mayor valor, más
        /// nítida la imagen. Solo es comparable con otras medidas de la misma cámara
        /// tomadas con la misma versión de la métrica.
        /// </summary>
        public double Focus { get; set; }

        /// <summary>Umbral aprendido para esta cámara, o null si no se ha calibrado.</summary>
        public double? FocusThreshold { get; set; }

        /// <summary>
        /// Veredicto de nitidez. Se calcula en el servidor para que el cliente no tenga
        /// que replicar el criterio.
        /// </summary>
        public FocusStatus FocusStatus { get; set; } = FocusStatus.Unknown;

        /// <summary>
        /// True solo cuando el enfoque alcanza un umbral calibrado. Sin umbral es false:
        /// se prefiere no afirmar que el enfoque es bueno cuando no hay con qué juzgarlo.
        /// Para distinguir "insuficiente" de "sin calibrar", usar FocusStatus.
        /// </summary>
        public bool IsFocusAcceptable { get; set; }

        /// <summary>Explicación cuando el estado no es Acceptable; null cuando lo es.</summary>
        public string? FocusMessage { get; set; }
    }

    /// <summary>Respuesta de POST /api/Focus/evaluate.</summary>
    public class FocusEvaluationDTO
    {
        public double Score { get; set; }
        public double? Threshold { get; set; }

        /// <summary>Veredicto de nitidez. Ver <see cref="FocusStatus"/>.</summary>
        public FocusStatus Status { get; set; } = FocusStatus.Unknown;

        /// <summary>
        /// True solo cuando Status es Acceptable. Sin umbral calibrado es false.
        /// </summary>
        public bool IsAcceptable { get; set; }

        public string? CameraName { get; set; }
        public string CapturedAt { get; set; } = default!;

        /// <summary>Mensaje listo para mostrar cuando el enfoque no es aceptable.</summary>
        public string? Message { get; set; }
    }

    /// <summary>Respuesta de POST /api/Focus/learn-threshold.</summary>
    public class FocusThresholdLearnedDTO
    {
        public string CameraName { get; set; } = default!;

        /// <summary>Cuántas capturas se promediaron.</summary>
        public int Samples { get; set; }

        /// <summary>Mediana de las capturas: la nitidez que alcanza este montaje enfocado.</summary>
        public double MeasuredSharpness { get; set; }

        /// <summary>Fracción de la mediana que se guardó como umbral (0-1).</summary>
        public double Margin { get; set; }

        /// <summary>Umbral guardado en settingsJson para esta cámara.</summary>
        public double Threshold { get; set; }

        /// <summary>Medidas individuales, para ver la dispersión entre capturas.</summary>
        public List<double> Readings { get; set; } = new();

        public string? Message { get; set; }
    }
}
