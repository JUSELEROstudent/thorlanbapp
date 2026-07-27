using OpenCvSharp;

namespace GotsThorlabs.Interfaces
{
    public interface ICameraService
    {
        /// <summary>
        /// Capture a single frame from a numeric camera index.
        /// </summary>
        /// <param name="cameraId">camera index</param>
        /// <returns>Captured frame as Mat (may be empty if capture failed)</returns>
        Mat CaptureFrame(int cameraId);

        /// <summary>
        /// Capture a single frame from the specified camera.
        /// Caller is responsible for disposing the returned Mat when done.
        /// </summary>
        /// <param name="localIdentifier">Device local identifier (serial number, display name, or numeric index string).</param>
        /// <returns>Captured frame as Mat (may be empty if capture failed)</returns>
        Mat CaptureFrame(string localIdentifier);

        /// <summary>
        /// Releases any persistent camera connection so the device is free for
        /// use by other software (e.g. the vendor's own app). Safe to call even
        /// when no connection is open. Implementations that open/close per
        /// capture may leave this as a no-op.
        /// </summary>
        void ReleaseConnection();

        /// <summary>
        /// Configura los parámetros de captura de un dispositivo a partir del JSON
        /// guardado en camera.settingsJson.
        ///
        /// Se llama ANTES de capturar (al abrir el streaming, al iniciar un recorrido
        /// y al arrancar una calibración automática). Los servicios de cámara son
        /// singleton y el DbContext es scoped, así que el driver no consulta la base
        /// de datos: quien llama ya tiene la entidad Camera cargada y le pasa el JSON.
        ///
        /// Es idempotente y tolerante a fallos: un JSON inválido o un parámetro que el
        /// dispositivo no soporte se registra en consola y se ignora, sin impedir la
        /// captura. Pasar null restablece el comportamiento por defecto del driver.
        /// </summary>
        /// <param name="localIdentifier">Identificador del dispositivo.</param>
        /// <param name="settingsJson">Contenido de camera.settingsJson, o null.</param>
        void ApplySettings(string localIdentifier, string? settingsJson);
    }
}
