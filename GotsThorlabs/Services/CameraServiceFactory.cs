using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;

namespace GotsThorlabs.Services
{
    /// <summary>
    /// Selects and provides the correct ICameraService implementation at runtime
    /// based on a driver string key ("generic" or "ids_peak_dotnet").
    /// Singleton: driver instances are reused and device-level locking is
    /// handled inside each service.
    /// </summary>
    public class CameraServiceFactory
    {
        private readonly Dictionary<string, ICameraService> _services;
        private readonly Dictionary<string, ICameraDiscoveryService> _discoveryServices;

        public CameraServiceFactory(
            VideoCaptureCameraService genericService,
            IdsPeakCameraService idsPeakService,
            IdsUEyeCameraService idsUEyeService)
        {
            _services = new Dictionary<string, ICameraService>(StringComparer.OrdinalIgnoreCase)
            {
                ["generic"]         = genericService,
                ["ids_peak_dotnet"] = idsPeakService,
                // Classic uEye USB/GigE cameras via the installed uEyeDotNet.dll driver.
                // Change DriverType to "ids_ueye" in the DB camera record to activate.
                ["ids_ueye"]        = idsUEyeService,
            };

            _discoveryServices = new Dictionary<string, ICameraDiscoveryService>(StringComparer.OrdinalIgnoreCase)
            {
                ["generic"]         = genericService,
                ["ids_peak_dotnet"] = idsPeakService,
                ["ids_ueye"]        = idsUEyeService,
            };
        }

        /// <summary>
        /// Returns the camera service for the requested driver key.
        /// Falls back to "generic" if the key is unknown or null.
        /// </summary>
        public ICameraService GetService(string? driverType)
        {
            if (!string.IsNullOrWhiteSpace(driverType)
                && _services.TryGetValue(driverType.Trim(), out var svc))
            {
                return svc;
            }
            return _services["generic"];
        }

        /// <summary>
        /// Returns the discovery service for the requested driver key.
        /// Falls back to "generic" if the key is unknown or null.
        /// </summary>
        public ICameraDiscoveryService GetDiscoveryService(string? driverType)
        {
            if (!string.IsNullOrWhiteSpace(driverType)
                && _discoveryServices.TryGetValue(driverType.Trim(), out var svc))
            {
                return svc;
            }
            return _discoveryServices["generic"];
        }

        /// <summary>
        /// Resolves a raw localIdentifier (moniker string, display name, numeric index, etc.)
        /// to the canonical stable identifier for the given driver (SerialNumber for IDS,
        /// numeric index string for generic/DirectShow).
        /// Returns null if no matching device is found.
        /// </summary>
        public string? ResolveLocalIdentifier(string? driverType, string? rawIdentifier)
        {
            if (string.IsNullOrWhiteSpace(rawIdentifier))
                return null;

            var discovery = GetDiscoveryService(driverType);
            var devices = discovery.EnumerateDevices().ToList();

            foreach (var d in devices)
            {
                // Exact match on SerialNumber or DisplayName
                if ((!string.IsNullOrEmpty(d.SerialNumber)
                        && string.Equals(d.SerialNumber, rawIdentifier, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrEmpty(d.DisplayName)
                        && string.Equals(d.DisplayName, rawIdentifier, StringComparison.OrdinalIgnoreCase))
                    // Substring: moniker/unique strings that contain serial or display name
                    || (!string.IsNullOrEmpty(d.SerialNumber)
                        && rawIdentifier.IndexOf(d.SerialNumber, StringComparison.OrdinalIgnoreCase) >= 0)
                    || (!string.IsNullOrEmpty(d.MonikerString)
                        && string.Equals(d.MonikerString, rawIdentifier, StringComparison.OrdinalIgnoreCase))
                    // Numeric index match
                    || (int.TryParse(rawIdentifier, out var idx) && d.Index == idx))
                {
                    // Return the most stable identifier available
                    return !string.IsNullOrEmpty(d.SerialNumber) ? d.SerialNumber : d.Index.ToString();
                }
            }

            return null;
        }
    }
}
