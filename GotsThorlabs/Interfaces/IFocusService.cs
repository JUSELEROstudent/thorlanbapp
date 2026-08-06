using GotsThorlabs.Models;

namespace GotsThorlabs.Interfaces
{
    /// <summary>
    /// Evalúa la nitidez de lo que está viendo una cámara en este momento.
    ///
    /// Se usa como verificación puntual antes de lanzar una calibración: calibrar con
    /// la muestra desenfocada produce mediciones de desplazamiento poco confiables,
    /// porque la correlación de fase necesita bordes definidos para encontrar el
    /// corrimiento entre dos imágenes.
    /// </summary>
    public interface IFocusService
    {
        /// <summary>
        /// Captura un cuadro de la cámara indicada (por nombre en la base de datos,
        /// aplicando su configuración guardada) y devuelve su medida de enfoque junto
        /// con el umbral calibrado.
        ///
        /// Si la cámara no tiene umbral calibrado el resultado es Unknown: sin una
        /// referencia medida en este montaje no hay forma de afirmar que el enfoque
        /// sea suficiente.
        /// </summary>
        Task<FocusEvaluationDTO> EvaluateAsync(string cameraName, CancellationToken ct);

        /// <summary>
        /// Aprende el umbral de nitidez de una cámara midiendo la muestra ya enfocada.
        ///
        /// El operador enfoca lo mejor que puede, se toman varias capturas, y se guarda
        /// como umbral una fracción de la mediana. Es la única forma de obtener un
        /// número con sentido: la nitidez no tiene escala absoluta, así que el criterio
        /// solo puede ser relativo a lo que este montaje óptico alcanza.
        /// </summary>
        /// <param name="cameraName">Nombre de la cámara en la base de datos.</param>
        /// <param name="samples">Capturas a promediar (3-15). Null usa el valor por defecto.</param>
        /// <param name="margin">Fracción de la nitidez medida a guardar (0.3-0.95). Null usa el valor por defecto.</param>
        Task<FocusThresholdLearnedDTO> LearnThresholdAsync(
            string cameraName, int? samples, double? margin, CancellationToken ct);
    }
}
