using GotsThorlabs.Models;

namespace GotsThorlabs.Interfaces
{
    /// <summary>
    /// Expone qué parámetros de captura acepta un driver de cámara y en qué rangos.
    ///
    /// Cada driver responde según lo que su SDK permita consultar: la uEye informa
    /// rangos reales del dispositivo, mientras que la cámara genérica (OpenCV /
    /// DirectShow) no tiene forma de consultarlos y devuelve los límites en nulo,
    /// caso en el que el frontend muestra un campo numérico libre.
    ///
    /// La lista se construye consultando el dispositivo, no la base de datos: es la
    /// capacidad del hardware. Lo que el usuario eligió vive en camera.settingsJson.
    /// </summary>
    public interface ICameraParameterProvider
    {
        /// <summary>
        /// Parámetros soportados por el dispositivo indicado, con sus rangos y el
        /// valor que tiene en este momento. Devuelve lista vacía si el dispositivo
        /// no está disponible; nunca lanza por ausencia de hardware.
        /// </summary>
        IReadOnlyList<CameraParameterDescriptor> GetParameters(string localIdentifier);
    }
}
