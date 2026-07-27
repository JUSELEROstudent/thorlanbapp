using System;
using System.Collections.Generic;

namespace GotsThorlabs.Database.EntityRepo.Entities
{
    public partial class Camera
    {
        public Camera()
        {
            GroupCalibrations = new HashSet<GroupCalibration>();
        }

        public string CameraId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string LocalIdentifier { get; set; } = null!;
        public string Features { get; set; } = null!;
        public string DriverType { get; set; } = null!;

        /// <summary>
        /// Parámetros de captura de esta cámara, serializados como JSON.
        /// Nulo mientras no se haya configurado nada (comportamiento por defecto del driver).
        ///
        /// Formato:
        /// {
        ///   "driverType": "ids_ueye",
        ///   "updatedAt": "2026-07-26T19:00:00Z",
        ///   "focusThreshold": "150",
        ///   "values": { "ExposureTime": "12.5", "MasterGain": "0" }
        /// }
        ///
        /// Los valores van como texto (con InvariantCulture) igual que Dx/Dy/Confidence
        /// en PicsCalibration; cada driver los interpreta según el tipo declarado en su
        /// descriptor de parámetros. No confundir con 'Features', que es texto libre
        /// descriptivo que escribe el usuario.
        /// </summary>
        public string? SettingsJson { get; set; }

        public virtual ICollection<GroupCalibration> GroupCalibrations { get; set; }
    }
}
