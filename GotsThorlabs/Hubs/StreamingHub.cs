using GotsThorlabs.BLL;
using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Models;
using GotsThorlabs.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenCvSharp;
using System.Drawing;
using System.Runtime.CompilerServices;
using Thorlabs.MotionControl.DeviceManagerCLI;
using Thorlabs.MotionControl.KCube.InertialMotorCLI;

namespace GotsThorlabs.Hubs
{
    public class StreamingHub : Hub
    {
        private readonly CameraServiceFactory _cameraFactory;
        private readonly ThorlabsDbContext _db;
        private static readonly Dictionary<string, CancellationTokenSource> _activeStreams = new();
        private static readonly object _lock = new();

        public StreamingHub(CameraServiceFactory cameraFactory, ThorlabsDbContext db)
        {
            _cameraFactory = cameraFactory;
            _db = db;
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            CancelStream(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        public void StopStream()
        {
            CancelStream(Context.ConnectionId);
        }

        private void CancelStream(string connectionId)
        {
            lock (_lock)
            {
                if (_activeStreams.TryGetValue(connectionId, out var cts))
                {
                    cts.Cancel();
                    cts.Dispose();
                    _activeStreams.Remove(connectionId);
                    Console.WriteLine($"[StreamingHub] Stream cancelled for connection {connectionId}");
                }
            }
        }

        /// <summary>
        /// Live preview stream.
        /// cameraName: name of the camera record in DB — used to resolve the correct driver.
        ///             Falls back to "generic" if no record is found.
        /// Stream stays alive until the client disconnects or cancellationToken is triggered.
        /// </summary>
        public async IAsyncEnumerable<byte[]> Counter(
         int camera,
         int delay,
         string? cameraName,
         [EnumeratorCancellation]
        CancellationToken cancellationToken)
        {
            var connectionId = Context.ConnectionId;

            CancelStream(connectionId);

            var streamCts = new CancellationTokenSource();
            lock (_lock)
            {
                _activeStreams[connectionId] = streamCts;
            }

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, Context.ConnectionAborted, streamCts.Token);
            var token = linkedCts.Token;

            var resolved = await ResolveCameraAsync(camera, cameraName);
            var cameraService = _cameraFactory.GetService(resolved.DriverType);

            // Se aplica la configuración guardada de la cámara antes de empezar a
            // capturar, para que el usuario vea en el streaming exactamente la misma
            // imagen que se usará al calibrar.
            ApplyCameraSettings(cameraService, resolved);

            Console.WriteLine($"[StreamingHub] Stream started — camera={cameraName ?? resolved.LocalIdentifier} driver={resolved.DriverType} connection={connectionId}");

            try
            {
                while (!token.IsCancellationRequested)
                {
                    byte[]? frameBytes = null;
                    try
                    {
                        using var image = cameraService.CaptureFrame(resolved.LocalIdentifier) ?? new Mat();

                        if (!image.Empty())
                            frameBytes = image.ToBytes();
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[StreamingHub] Frame capture error: {ex.Message} — retrying...");
                    }

                    if (frameBytes != null)
                    {
                        yield return frameBytes;
                        await Task.Delay(delay, token).ConfigureAwait(false);
                    }
                    else
                    {
                        await Task.Delay(100, token).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                CancelStream(connectionId);
                Console.WriteLine($"[StreamingHub] Stream stopped — camera={cameraName ?? resolved.LocalIdentifier} connection={connectionId}");
            }
        }

        /// <summary>
        /// Igual que Counter, pero cada cuadro viaja acompañado de su medida de
        /// enfoque (varianza del Laplaciano), para que el investigador pueda enfocar
        /// mirando un número en vez de a ojo.
        ///
        /// Se calcula sobre el MISMO cuadro que ya se estaba capturando para el
        /// preview: abrir una segunda conexión al dispositivo competiría con este
        /// streaming y con las capturas de calibración.
        ///
        /// Es un método aparte y no un cambio en Counter porque Counter lo consumen
        /// también index.vue y signalrtest.vue, que esperan recibir el cuadro pelado.
        /// </summary>
        public async IAsyncEnumerable<StreamFrameDTO> CounterWithMetrics(
            int camera,
            int delay,
            string? cameraName,
            [EnumeratorCancellation]
            CancellationToken cancellationToken)
        {
            var connectionId = Context.ConnectionId;

            CancelStream(connectionId);

            var streamCts = new CancellationTokenSource();
            lock (_lock)
            {
                _activeStreams[connectionId] = streamCts;
            }

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, Context.ConnectionAborted, streamCts.Token);
            var token = linkedCts.Token;

            var resolved = await ResolveCameraAsync(camera, cameraName);
            var cameraService = _cameraFactory.GetService(resolved.DriverType);

            ApplyCameraSettings(cameraService, resolved);

            // El criterio se lee una sola vez: deserializar settingsJson en cada cuadro
            // costaría decenas de parseos por segundo para un valor que no cambia
            // mientras dura el stream.
            var criterion = FocusCriterion.FromSettings(resolved.SettingsJson);

            Console.WriteLine($"[StreamingHub] Stream con métricas iniciado — camera={cameraName ?? resolved.LocalIdentifier} driver={resolved.DriverType} connection={connectionId}");

            try
            {
                while (!token.IsCancellationRequested)
                {
                    StreamFrameDTO? payload = null;
                    try
                    {
                        using var image = cameraService.CaptureFrame(resolved.LocalIdentifier) ?? new Mat();

                        if (!image.Empty())
                        {
                            var focus = FocusMetrics.Sharpness(image);
                            var status = criterion.Evaluate(focus);
                            payload = new StreamFrameDTO
                            {
                                Frame = image.ToBytes(),
                                Focus = focus,
                                FocusThreshold = criterion.Threshold,
                                FocusStatus = status,
                                IsFocusAcceptable = status == FocusStatus.Acceptable,
                                FocusMessage = criterion.DescribeFailure(focus)
                            };
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[StreamingHub] Frame capture error: {ex.Message} — retrying...");
                    }

                    if (payload != null)
                    {
                        yield return payload;
                        await Task.Delay(delay, token).ConfigureAwait(false);
                    }
                    else
                    {
                        await Task.Delay(100, token).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                CancelStream(connectionId);
                Console.WriteLine($"[StreamingHub] Stream con métricas detenido — connection={connectionId}");
            }
        }

        // ── Resolución de cámara compartida por ambos streams ────────────

        private sealed class ResolvedCamera
        {
            public string DriverType { get; set; } = "generic";
            public string LocalIdentifier { get; set; } = string.Empty;
            public string? SettingsJson { get; set; }
        }

        private async Task<ResolvedCamera> ResolveCameraAsync(int camera, string? cameraName)
        {
            var resolved = new ResolvedCamera
            {
                DriverType = "generic",
                LocalIdentifier = camera.ToString()
            };

            if (string.IsNullOrWhiteSpace(cameraName))
                return resolved;

            var cameraRecord = await _db.Cameras
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name.ToLower() == cameraName.Trim().ToLower());

            if (cameraRecord != null)
            {
                resolved.DriverType = cameraRecord.DriverType;
                resolved.SettingsJson = cameraRecord.SettingsJson;
                if (!string.IsNullOrWhiteSpace(cameraRecord.LocalIdentifier))
                    resolved.LocalIdentifier = cameraRecord.LocalIdentifier;
            }

            return resolved;
        }

        private static void ApplyCameraSettings(Interfaces.ICameraService cameraService, ResolvedCamera resolved)
        {
            try
            {
                cameraService.ApplySettings(resolved.LocalIdentifier, resolved.SettingsJson);
            }
            catch (Exception ex)
            {
                // Una configuración que el dispositivo rechace no debe impedir ver el
                // streaming: se registra y se sigue con los valores por defecto.
                Console.WriteLine($"[StreamingHub] No se pudo aplicar la configuración de la cámara: {ex.Message}");
            }
        }

    }

    public class UpdateStatus : Hub
    {
        private readonly ThorlabsDbContext _db;
        private readonly CameraServiceFactory _cameraFactory;

        public UpdateStatus(ThorlabsDbContext db, CameraServiceFactory cameraFactory)
        {
            _db = db;
            _cameraFactory = cameraFactory;
        }

        /// <summary>
        /// Starts a mosaic tour using the camera associated with the groupCalibration.
        /// The camera's DriverType field determines which capture service is used.
        /// </summary>
        public async IAsyncEnumerable<dynamic> Imgupdate(
          int indexcam,
          decimal areaX_mm,
          decimal areaY_mm,
          string groupCalibrationId,
          string device,
         [EnumeratorCancellation]
        CancellationToken cancellationToken)
        {
            // Resolve camera driver from the group calibration -> camera entity
            var groupCalibration = await _db.GroupCalibrations
                .AsNoTracking()
                .Include(g => g.Camera)
                .FirstOrDefaultAsync(g => g.GroupCailbrationId == groupCalibrationId);

            var driverType = groupCalibration?.Camera?.DriverType ?? "generic";
            var resolvedLocalIdentifier = groupCalibration?.Camera?.LocalIdentifier
                ?? throw new HubException("No se encontró la cámara asociada a la calibración.");
            var cameraService = _cameraFactory.GetService(driverType);

            // Las imágenes del recorrido deben tomarse con la misma configuración con
            // la que se calibró; de lo contrario el desplazamiento medido en la
            // calibración no corresponde al de estas capturas.
            try
            {
                cameraService.ApplySettings(resolvedLocalIdentifier, groupCalibration.Camera?.SettingsJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateStatus] No se pudo aplicar la configuración de la cámara: {ex.Message}");
            }

            var controlmotor = new TakeTour(resolvedLocalIdentifier, _db, cameraService);
            var processimgs = controlmotor.Createmosaicstepbystep(areaX_mm, areaY_mm, device.Trim(), groupCalibrationId);
            await foreach (var url in processimgs)
            {
                yield return url;
            }
        }
    }
    //        var controlmotor = new Tim101_4_ch_inertial_motor();
    //        var processimgs = controlmotor.CreatesticherOpencv(indexcam, 2, "97000001");
    //        await foreach (var url in processimgs)
    //        {
    //            yield return url;
    //        }
    //        //creacion del metodo que se encarga de actualizar el estado del mapeo por imagenes en la app
    //        //SE PUEDE MEJORAR EL CODIGO METIENDO TODO EN LA CLASE 101_4 .... Y USANDO YIELD EN EL WHILE
    //    }
    //}
    public class resourcesignal {
        public string Name { get; set; }
        // public CancellationToken cancelacion { get; set; }
        public int numero { get; set; }
        public Bitmap imagen { get; set; }

    }
}
