using System.Globalization;
using GotsThorlabs.Models;

namespace GotsThorlabs.BLL
{
    /// <summary>
    /// El criterio con el que se juzga la nitidez de una cámara: el umbral aprendido
    /// más la decisión de si ese umbral es utilizable.
    ///
    /// Se construye una vez a partir de settingsJson y luego se aplica a cada medida.
    /// Importa para el streaming, donde evaluar cuadro a cuadro implicaría volver a
    /// deserializar el JSON de configuración decenas de veces por segundo.
    ///
    /// Centralizar aquí el criterio evita que el hub y el servicio de enfoque decidan
    /// cada uno por su lado qué cuenta como "suficientemente nítido" y se desincronicen.
    /// </summary>
    public sealed class FocusCriterion
    {
        private const string NotCalibratedMessage =
            "Esta cámara no tiene umbral de nitidez calibrado, así que no se puede afirmar si el " +
            "enfoque es suficiente. Enfoque la muestra lo mejor posible y ejecute «calibrar umbral» " +
            "para fijar la referencia de este montaje.";

        private const string StaleMetricMessage =
            "El umbral guardado se midió con una versión anterior del cálculo de nitidez y está en " +
            "otra escala, por lo que compararlo daría un veredicto falso. Vuelva a ejecutar " +
            "«calibrar umbral» para esta cámara.";

        private const string InvalidThresholdMessage =
            "El umbral de nitidez guardado para esta cámara no es un número válido. Vuelva a " +
            "ejecutar «calibrar umbral».";

        private const string SettingsChangedMessage =
            "Los parámetros de captura de esta cámara cambiaron desde que se calibró el umbral. " +
            "La exposición y la ganancia afectan a la medida de nitidez, así que el umbral anterior " +
            "ya no aplica. Vuelva a ejecutar «calibrar umbral» con la configuración actual.";

        private readonly string? _unavailableReason;

        private FocusCriterion(double? threshold, string? unavailableReason)
        {
            Threshold = threshold;
            _unavailableReason = unavailableReason;
        }

        /// <summary>Umbral utilizable, o null si esta cámara no está calibrada.</summary>
        public double? Threshold { get; }

        /// <summary>True cuando hay un umbral con el que se puede emitir un veredicto.</summary>
        public bool IsCalibrated => Threshold.HasValue;

        /// <summary>
        /// Lee el criterio de settingsJson. Un umbral solo se acepta si viene marcado con
        /// la misma versión de métrica que produce <see cref="FocusMetrics.Sharpness"/>:
        /// los umbrales anteriores al normalizado se midieron en otra escala y aplicarlos
        /// tal cual aprobaría o rechazaría imágenes por un número que ya no significa lo
        /// mismo. En ese caso se devuelve un criterio sin calibrar, que reporta Unknown.
        /// </summary>
        public static FocusCriterion FromSettings(string? settingsJson)
        {
            var settings = CameraSettings.Parse(settingsJson);

            if (settings == null || string.IsNullOrWhiteSpace(settings.FocusThreshold))
                return new FocusCriterion(null, NotCalibratedMessage);

            if (!string.Equals(settings.FocusMetricVersion, FocusMetrics.MetricVersion, StringComparison.Ordinal))
                return new FocusCriterion(null, StaleMetricMessage);

            if (!string.Equals(settings.FocusSettingsFingerprint, settings.Fingerprint(), StringComparison.Ordinal))
                return new FocusCriterion(null, SettingsChangedMessage);

            if (!double.TryParse(settings.FocusThreshold, NumberStyles.Float, CultureInfo.InvariantCulture, out var threshold))
                return new FocusCriterion(null, InvalidThresholdMessage);

            return new FocusCriterion(threshold, null);
        }

        /// <summary>Aplica el criterio a una medida de nitidez.</summary>
        public FocusStatus Evaluate(double score)
        {
            if (!Threshold.HasValue) return FocusStatus.Unknown;
            return score >= Threshold.Value ? FocusStatus.Acceptable : FocusStatus.Insufficient;
        }

        /// <summary>
        /// Mensaje para mostrar al usuario, o null cuando el enfoque es aceptable.
        /// </summary>
        public string? DescribeFailure(double score)
        {
            if (!Threshold.HasValue) return _unavailableReason;
            if (score >= Threshold.Value) return null;

            return $"El enfoque medido ({score:F1}) está por debajo del umbral calibrado para esta cámara " +
                   $"({Threshold.Value:F1}). Ajuste el enfoque del microscopio antes de calibrar: una muestra " +
                   "desenfocada hace que la correlación de fase mida desplazamientos poco confiables.";
        }
    }
}
