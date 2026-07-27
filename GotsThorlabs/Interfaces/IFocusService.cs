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
        /// con el umbral configurado.
        /// </summary>
        Task<FocusEvaluationDTO> EvaluateAsync(string cameraName, CancellationToken ct);
    }
}
