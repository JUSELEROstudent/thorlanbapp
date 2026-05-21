namespace GotsThorlabs.Models
{
    /// <summary>
    /// Represents a physical camera device discovered at runtime.
    /// </summary>
    public class DeviceInfo
    {
        /// <summary>Zero-based index in the enumerated device list.</summary>
        public int Index { get; set; }

        /// <summary>
        /// Most reliable unique identifier for the device.
        /// For IDS cameras: SerialNumber from the IDS Peak SDK.
        /// For DirectShow cameras: numeric index as string (the only stable ID).
        /// </summary>
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>Human-readable name / display name.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>DirectShow moniker string (only populated for DirectShow/generic devices).</summary>
        public string MonikerString { get; set; } = string.Empty;
    }
}
