using GotsThorlabs.Models;

namespace GotsThorlabs.Interfaces
{
    /// <summary>
    /// Enumerates physical camera devices available for a specific driver type.
    /// </summary>
    public interface ICameraDiscoveryService
    {
        /// <summary>
        /// Returns information about all currently connected devices.
        /// </summary>
        IEnumerable<DeviceInfo> EnumerateDevices();
    }
}
