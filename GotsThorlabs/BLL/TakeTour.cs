using GotsThorlabs.Interfaces;
using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Database.EntityRepo.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenCvSharp;
using OpenCvSharp.Detail;
using OpenCvSharp.Internal.Vectors;
using System.Collections;
//using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Thorlabs.MotionControl.DeviceManagerCLI;
using Thorlabs.MotionControl.KCube.InertialMotorCLI;

namespace GotsThorlabs.BLL
{
    ///<summary>
    ///Clase de implementacion para el dispositivo Kim 101 de 4 canales y especificamente para el caso de un nuevo tour
    ///</summary>
    ///<remarks>
    ///crea un objeto con las utilidades para usar el dispositivo kim 101 4ch, ejecutar los metods de acuerdo al orden 
    /// 1. builddeviceslist() 2. deviceslist()
    ///</remarks>
    public class TakeTour : ITakeTour
    {
        Dictionary<int, InertialMotorStatus.MotorChannels> chanelsDevice = new Dictionary<int, InertialMotorStatus.MotorChannels>()
                {
                    {1, InertialMotorStatus.MotorChannels.Channel1},
                    {2, InertialMotorStatus.MotorChannels.Channel2},
                    {3, InertialMotorStatus.MotorChannels.Channel3},
                    {4, InertialMotorStatus.MotorChannels.Channel4}
                };

        private readonly ThorlabsDbContext _db;
        private readonly GotsThorlabs.Interfaces.ICameraService _cameraService;
        private long? _currentTourId;

        /// <summary>Número de imágenes a lo largo del eje Y (filas). Se calcula dinámicamente.</summary>
        public int rows; //= i  (eje Y / Channel2)
        /// <summary>Número de imágenes a lo largo del eje X (columnas). Se calcula dinámicamente.</summary>
        public int columns; // = j  (eje X / Channel1)
        /// <summary>LocalIdentifier of the camera resolved from GroupCalibration → Camera in the DB.</summary>
        public string localIdentifier;
        string namefolder;
        private decimal _areaX_mm;
        private decimal _areaY_mm;

        public Mat[] image;
        public Mat[] finalimg;
        public Mat mosaic;// imagen general ya con el tamaño completo para la imagen final que laverga a todas las sub imagenes

        public TakeTour(string LocalIdentifier, ThorlabsDbContext db, GotsThorlabs.Interfaces.ICameraService cameraService)
        {
            localIdentifier = LocalIdentifier;
            namefolder = Utilities.getTimeInString();
            _db = db;
            _cameraService = cameraService;
            // rows, columns, image, finalimg, mosaic se inicializan en Createmosaicstepbystep
            // una vez calculado el grid a partir del área (mm) y la calibración.
            mosaic = new Mat();
        }

       
        public async IAsyncEnumerable<dynamic> Createmosaicstepbystep(decimal areaX_mm, decimal areaY_mm, string kimDeviceId, string groupCalibrationId, SweepPattern sweepPattern = SweepPattern.SerpentineScaled)
        {
            _areaX_mm = areaX_mm;
            _areaY_mm = areaY_mm;

            var developerurl = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
            var listado = deviceslist();
            string path = Environment.CurrentDirectory;
            if (listado == null) { yield return false; }
            if (!listado.Contains(kimDeviceId)) { throw new HubException("Error al connectarse al dispositivo kim04 [ES]"); }
            var developerurl2 = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
            var urlslocals = developerurl2.Split(";");


            KCubeInertialMotor deviceconnect = KCubeInertialMotor.CreateKCubeInertialMotor(kimDeviceId);
            try
            {
                if (!deviceconnect.IsConnected)
                {
                    deviceconnect.Disconnect(true);
                    //agregaun delay para esperar a qeu este desconectad 

                }
                    // Open a connection to  devices.
                deviceconnect.Connect(kimDeviceId);
            }
            catch (Exception)
            {
                deviceconnect.Disconnect(true);
                throw new HubException("Error al establecer al dispositivo kim04 [ES]");
            }
            if (!deviceconnect.IsSettingsInitialized())
            {
                try
                {
                    deviceconnect.WaitForSettingsInitialized(500);
                }
                catch (Exception)
                {
                    //throw new HubException("Error Inicializar estado en dispositivo [ES]");
                }
            }

            deviceconnect.StartPolling(250);
            Thread.Sleep(500);
            deviceconnect.EnableDevice();
            Thread.Sleep(500);

            InertialMotorConfiguration InertialMotorConfiguration = deviceconnect.GetInertialMotorConfiguration(kimDeviceId);
            ThorlabsInertialMotorSettings currentDeviceSettings = ThorlabsInertialMotorSettings.GetSettings(InertialMotorConfiguration);

            // Set the 'Step' paramaters for the Inertia Motor and download to device.
            // IMPORTANTE: se configuran AMBOS canales (X=Channel1, Y=Channel2) con los
            // MISMOS valores. Antes solo se tocaba el canal 1 (X); el canal 2 (Y) se
            // quedaba con lo que tuviera guardado el dispositivo (de Kinesis, de una
            // calibración anterior, o de un movimiento manual hecho desde /movedevice
            // en NodeHomepage.cs), pudiendo tener un StepRate/StepAcceleration distinto
            // al de X sin que nadie lo notara — eso hace que el eje Y se mueva con un
            // comportamiento diferente (velocidad/aceleración/overshoot) al eje X.
            // Estos valores DEBEN coincidir con los que usa
            // PicsCalibrationService.RunAutoCalibrationAsync: si el motor se mueve aquí
            // con una configuración distinta a la que se usó al calibrar, la relación
            // píxeles/step medida en la calibración deja de ser válida para el tour real.
            // Los parámetros salen de la caracterización mecánica del grupo (tabla
            // motorCalibration) y ya no de literales repetidos en dos archivos. Esa
            // duplicación era el riesgo: si el valor del recorrido y el de la calibración
            // divergían, la relación píxeles/paso quedaba medida para un comportamiento
            // del motor distinto del que se usaba después, sin ninguna señal visible.
            //
            // .Trim() defensivo: evita que un espacio en blanco accidental en el valor que
            // llega del cliente SignalR haga que el filtro no matchee ningún registro.
            var groupCalibrationIdTrimmed = groupCalibrationId?.Trim() ?? groupCalibrationId;

            var motorCalibration = _db.MotorCalibrations
                .AsNoTracking()
                .Include(m => m.AxisStepCalibrations)
                .Where(m => m.GroupCailbrationId == groupCalibrationIdTrimmed)
                .OrderByDescending(m => m.Acepted)
                .ThenByDescending(m => m.Date)
                .FirstOrDefault();

            int stepRate = (int)(motorCalibration?.StepRate ?? 200);
            int stepAcceleration = (int)(motorCalibration?.StepAcceleration ?? 100);

            if (motorCalibration == null)
            {
                Console.WriteLine("[TakeTour] AVISO: el grupo no tiene caracterización mecánica del motor. " +
                    "Se usan los valores por defecto (StepRate=200, StepAcceleration=100) y el tamaño de paso " +
                    "nominal de 30 nm SIN VERIFICAR: el área recorrida en milímetros puede no corresponder " +
                    "con la solicitada.");
            }

            currentDeviceSettings.Drive.Channel(chanelsDevice[1]).StepRate = stepRate;
            currentDeviceSettings.Drive.Channel(chanelsDevice[1]).StepAcceleration = stepAcceleration;
            currentDeviceSettings.Drive.Channel(chanelsDevice[2]).StepRate = stepRate;
            currentDeviceSettings.Drive.Channel(chanelsDevice[2]).StepAcceleration = stepAcceleration;
            deviceconnect.SetSettings(currentDeviceSettings, true, true);



            Decimal newPos = deviceconnect.GetPosition(InertialMotorStatus.MotorChannels.Channel1);
            var rand = new Random();

            // SECCIÓN CALCULO DE GRID BASADO EN CALIBRACIÓN
            // Se obtienen todas las calibraciones del grupo y se calculan los parámetros
            // del grid (número de imágenes, espaciado del motor, overlap) en base al área
            // deseada (mm) y a la mejor calibración disponible por eje.
            var allCalibrations = _db.PicsCalibrations.AsNoTracking().Where(x => x.GroupCailbrationId == groupCalibrationIdTrimmed).ToList();

            // Diagnóstico: deja registro explícito de con qué GroupCalibrationId llegó la
            // petición y cuántas calibraciones se encontraron para ese grupo. Sirve para
            // validar en logs, ante cualquier duda, que el filtro está usando el valor
            // correcto (el que mandó el cliente) y no está trayendo datos de otro grupo.
            Console.WriteLine($"[TakeTour] Tour solicitado con groupCalibrationId='{groupCalibrationIdTrimmed}' " +
                $"(area {areaX_mm}x{areaY_mm} mm) -> {allCalibrations.Count} registros de calibración encontrados " +
                $"para ese grupo: [{string.Join(", ", allCalibrations.Select(c => $"{c.AxisMovementName}/{c.MovementValue}steps/dx={c.Dx}/dy={c.Dy}"))}]");

            int frameWidth;
            int frameHeight;
            try
            {
                using var probeFrame = _cameraService?.CaptureFrame(localIdentifier);
                // BUG ORIGINAL: 'probeFrame?.Rows ?? 1080' solo cae al valor por defecto
                // cuando probeFrame ES NULL. Si la cámara devuelve un Mat válido pero
                // VACÍO (Rows=0, Cols=0) — típico cuando el driver falla al capturar pero
                // no retorna null — 'probeFrame?.Rows' se evalúa a 0 (no a null), así que
                // el '??' nunca se dispara y frameHeight/frameWidth quedan en 0. Con
                // frameWidth=frameHeight=0, el avance entre imágenes (30% del frame) es 0,
                // MotorStepX/Y se redondean a 0 y quedan forzados al mínimo de 1 step, y
                // con el nominal de 30 nm/step eso dispara ImagesX/Y a decenas de miles
                // para cubrir el área pedida — exactamente el grid de 66668x66668 que
                // reventó la asignación de memoria en TakeAPic.
                // Fix: se exige explícitamente Rows/Cols > 0, igual que ya se hace
                // correctamente más abajo en TakeAPic (línea 'ownedFrame.Rows > 0 ? ... ').
                bool probeFrameValido = probeFrame != null && probeFrame.Rows > 0 && probeFrame.Cols > 0;
                frameHeight = probeFrameValido ? probeFrame.Rows : 1080;
                frameWidth = probeFrameValido ? probeFrame.Cols : 1920;
                if (!probeFrameValido)
                {
                    Console.WriteLine($"[TakeTour] ADVERTENCIA: el frame de prueba de la cámara vino nulo o vacío " +
                        $"(localIdentifier='{localIdentifier}'). Se usará resolución por defecto {frameWidth}x{frameHeight}px " +
                        $"para calcular el grid — si la cámara real tiene otra resolución, el grid calculado no será preciso. " +
                        $"Revise que la cámara esté disponible/no esté ocupada por otro proceso antes de iniciar el tour.");
                }
            }
            catch
            {
                frameHeight = 1080;
                frameWidth = 1920;
            }

            // Tamaño de paso medido con pie de rey, por eje. Si el grupo no tiene esa
            // caracterización, Calculate cae al nominal de 30 nm y lo reporta en el
            // resultado para que quede constancia de que el área en mm es una estimación.
            // Se usa el paso de IDA y no la media: el eje X solo avanza en sentido
            // positivo durante todo el recorrido, y en Y la distancia que cubre cada
            // columna se define por la ida (la vuelta se compensa con más pasos).
            double? stepNmX = ReadMeasuredStepNm(motorCalibration, "x", forward: true);
            double? stepNmY = ReadMeasuredStepNm(motorCalibration, "y", forward: true);
            double? stepNmYBack = ReadMeasuredStepNm(motorCalibration, "y", forward: false);
            // El sentido de vuelta de X no interviene en el cálculo del grid (X nunca
            // retrocede mientras se toman imágenes) pero sí en el regreso al origen del
            // final, que sí es un movimiento hacia atrás y rinde menos por paso.
            double? stepNmXBack = ReadMeasuredStepNm(motorCalibration, "x", forward: false);

            MosaicGridCalculator.GridResult grid;
            try
            {
                grid = MosaicGridCalculator.Calculate(
                    areaX_mm, areaY_mm, allCalibrations, frameWidth, frameHeight,
                    stepNmX, stepNmY, stepNmYBack);
            }
            catch
            {
                // El KIM ya está tomado en este punto. Si el grid no se puede calcular
                // hay que soltarlo antes de propagar, o queda ocupado y el siguiente
                // intento falla al conectar sin relación aparente con la causa real.
                try { deviceconnect.StopPolling(); deviceconnect.Disconnect(true); } catch { }
                throw;
            }

            foreach (var advertencia in grid.Warnings)
                Console.WriteLine($"[TakeTour] AVISO DE CALIBRACIÓN: {advertencia}");

            Console.WriteLine($"[TakeTour] Patrón de barrido: {sweepPattern}. " +
                $"MotorStepY ida={grid.MotorStepY}, vuelta={grid.MotorStepYBackward} " +
                $"(compensación {(grid.BackwardStepMeasured ? "medida" : "no disponible")}).");

            if (sweepPattern == SweepPattern.SerpentineScaled && !grid.BackwardStepMeasured)
            {
                Console.WriteLine("[TakeTour] AVISO: se pidió serpentina escalada pero el eje Y no tiene " +
                    "medido el sentido de vuelta. Se comporta como serpentina simple: las columnas de " +
                    "vuelta quedarán más cortas que las de ida.");
            }

            if (!grid.StepSizeMeasured)
            {
                Console.WriteLine("[TakeTour] AVISO: tamaño de paso SIN VERIFICAR " +
                    $"(X={grid.StepSizeNmX:G6} nm, Y={grid.StepSizeNmY:G6} nm, nominal). " +
                    "El recorrido funciona igual, pero el área cubierta en milímetros es una " +
                    "estimación. Ejecute la medición con pie de rey para este grupo.");
            }

            Console.WriteLine($"[TakeTour] Grid calculado: ImagesX={grid.ImagesX}, ImagesY={grid.ImagesY}, " +
                $"frame={frameWidth}x{frameHeight}px, MotorStepX={grid.MotorStepX}, MotorStepY={grid.MotorStepY}, " +
                $"PxPerStepX={grid.PxPerStepX:G6}, PxPerStepY={grid.PxPerStepY:G6}, " +
                $"StepMmX={grid.StepMmX:G6}, StepMmY={grid.StepMmY:G6}.");

            // Estimación previa. El tiempo de cada movimiento ya se conoce en este punto,
            // así que se deja registrado antes de empezar en lugar de descubrir a mitad
            // del recorrido que faltaba una hora.
            var estimate = TourTimeEstimator.Calculate(grid, sweepPattern, stepRate);
            Console.WriteLine($"[TakeTour] Estimación: {estimate.TotalImages} imágenes, " +
                $"{estimate.TotalSeconds / 60:F1} min en total ({estimate.MotionSeconds / 60:F1} de motor, " +
                $"{estimate.CaptureSeconds / 60:F1} de captura). Movimiento más largo: " +
                $"{estimate.LongestMoveSteps} pasos ≈ {estimate.LongestMoveSeconds:F0}s a {estimate.StepRate} pasos/s.");
            foreach (var advertencia in estimate.Warnings)
                Console.WriteLine($"[TakeTour] AVISO DE PLANIFICACIÓN: {advertencia}");

            // Inicializar arrays dinámicamente con el grid calculado.
            // columns = eje X (Channel1), rows = eje Y (Channel2)
            columns = grid.ImagesX;
            rows = grid.ImagesY;
            image = new Mat[rows];
            finalimg = new Mat[grid.ImagesX];
            mosaic = new Mat();
            // fin inicialización grid

            // ORIGEN DEL RECORRIDO
            // El KIM101 no tiene referencia absoluta —no hay encoder ni se usan finales
            // de carrera—, así que el origen de un recorrido es, por definición, el punto
            // donde está la platina al empezar. Eso no se decía en ninguna parte y el
            // recorrido comandaba posiciones absolutas contra el contador heredado del
            // recorrido anterior. Con eso el primer movimiento acababa siendo un viaje de
            // más de cien mil pasos (varios minutos), y encima terminaba en un punto que
            // ya no era el origen físico, porque el contador se había desplazado respecto
            // de la platina durante el recorrido previo (ver AxisTravel). Redefinir el
            // cero aquí hace explícito lo que el recorrido ya daba por supuesto y elimina
            // ese viaje inicial: no mueve el motor, solo reetiqueta la posición actual.
            int origenX, origenY;
            try
            {
                int heredadoX = deviceconnect.GetPosition(chanelsDevice[1]);
                int heredadoY = deviceconnect.GetPosition(chanelsDevice[2]);
                deviceconnect.SetPositionToZero(chanelsDevice[1]);
                deviceconnect.SetPositionToZero(chanelsDevice[2]);
                origenX = deviceconnect.GetPosition(chanelsDevice[1]);
                origenY = deviceconnect.GetPosition(chanelsDevice[2]);
                Console.WriteLine($"[TakeTour] Origen fijado en la posición actual de la platina. " +
                    $"Contador heredado X={heredadoX}, Y={heredadoY} -> ahora X={origenX}, Y={origenY}.");
            }
            catch (Exception ex)
            {
                // Todavía no hay fila de tour que marcar, pero el KIM sí está tomado: hay
                // que soltarlo o queda ocupado hasta reiniciar la aplicación.
                try { deviceconnect.StopPolling(); deviceconnect.Disconnect(true); } catch { }
                throw new HubException($"No se pudo fijar el origen del recorrido en el dispositivo KIM: {ex.Message}");
            }
            if (origenX != 0 || origenY != 0)
                Console.WriteLine($"[TakeTour] ADVERTENCIA: el contador no quedó en cero al redefinir el origen " +
                    $"(X={origenX}, Y={origenY}). El recorrido continúa, pero el regreso al origen del final " +
                    $"puede quedar desplazado.");

            // Seguimiento de la posición FÍSICA, que deja de coincidir con el contador en
            // cuanto los dos sentidos rinden distinto. Es lo que permite deshacer al final
            // el desplazamiento real y no el contable.
            var travelX = new AxisTravel(
                stepNmX ?? MosaicGridCalculator.NmPerStep,
                stepNmXBack ?? stepNmX ?? MosaicGridCalculator.NmPerStep,
                origenX);
            var travelY = new AxisTravel(
                stepNmY ?? MosaicGridCalculator.NmPerStep,
                stepNmYBack ?? stepNmY ?? MosaicGridCalculator.NmPerStep,
                origenY);

            string fullnamefolder = CreateTour(grid.CalibrationX.PicsCalibrationId);
            bool tourCompleted = false;

            // RECORRIDO EN PATRÓN "S":
            // - El eje X (Channel1) siempre avanza (j * MotorStepX).
            // - El eje Y (Channel2) alterna dirección por columna:
            //      columnas pares   → sube  (i_motor: 0 → ImagesY-1)
            //      columnas impares → baja  (i_motor: ImagesY-1 → 0)
            //   El almacenamiento en image[] siempre usa el índice espacial k
            //   para que VConcat preserve el orden espacial correcto.
            // Posición comandada del eje Y al empezar la columna actual. Se lleva a mano
            // porque con los pasos escalados por sentido las posiciones ya no son
            // múltiplos de un único MotorStepY.
            long yColumnStart = origenY;
            long yColumnEnd = origenY;

            try
            {
                for (int j = 0; j < grid.ImagesX; j++)
                {
                    long xTarget = origenX + (long)j * grid.MotorStepX;
                    var moveX = MotorMotion.MoveAndWait(deviceconnect, chanelsDevice[1], (int)xTarget, stepRate);
                    if (!moveX.Ok)
                        throw new Exception(
                            $"Recorrido detenido en la columna {j + 1} de {grid.ImagesX}. {moveX.Describe("X")}");
                    travelX.Record(moveX.PositionReached);

                    // El unidireccional recorre siempre en positivo; las serpentinas alternan.
                    bool descending = sweepPattern != SweepPattern.Unidirectional && (j % 2 == 1);

                    // Solo la serpentina escalada compensa el sentido; las otras dos usan el
                    // mismo número de pasos en ambos.
                    int stepY = (descending && sweepPattern == SweepPattern.SerpentineScaled)
                        ? grid.MotorStepYBackward
                        : grid.MotorStepY;

                    for (int k = 0; k < grid.ImagesY; k++)
                    {
                        // k es el orden de visita; la fila FÍSICA es la que decide dónde se
                        // guarda. Antes se almacenaba por orden de visita, de modo que las
                        // columnas descendentes quedaban invertidas de arriba abajo respecto
                        // de las ascendentes y el mosaico salía con columnas espejadas.
                        int spatialRow = descending ? (grid.ImagesY - 1 - k) : k;
                        long yTarget = yColumnStart + (descending ? -(long)k * stepY : (long)k * stepY);

                        var moveY = MotorMotion.MoveAndWait(deviceconnect, chanelsDevice[2], (int)yTarget, stepRate);
                        if (!moveY.Ok)
                            throw new Exception(
                                $"Recorrido detenido en la columna {j + 1} de {grid.ImagesX}, " +
                                $"fila {k + 1} de {grid.ImagesY}. {moveY.Describe("Y")}");
                        travelY.Record(moveY.PositionReached);

                        if (k == grid.ImagesY - 1) yColumnEnd = yTarget;

                        string pathsave = TakeAPic("unitofpics", fullnamefolder, j, spatialRow, 0);
                        var splitpathdir = pathsave.Split($"{Path.DirectorySeparatorChar}");
                        int dimpath = splitpathdir.Length;
                        var namephotounits = splitpathdir[dimpath - 1];
                        var urlunitpi = urlslocals[2] + $"/SouerceStaticFiles/{namefolder}/" + namephotounits + "?ranmd=" + rand.Next().ToString();
                        yield return urlunitpi;
                    }
                    // Punto de partida de la siguiente columna.
                    if (sweepPattern == SweepPattern.Unidirectional)
                    {
                        // Hay que volver físicamente al inicio de la columna. Comandar los
                        // mismos pasos que se subieron dejaría el eje corto, porque el
                        // retroceso rinde menos: se comanda la cantidad equivalente en
                        // distancia usando el paso de vuelta.
                        long returnSteps = (long)(grid.ImagesY - 1) * grid.MotorStepYBackward;
                        long yReturn = yColumnEnd - returnSteps;

                        if (j < grid.ImagesX - 1)
                        {
                            // Este es el movimiento más largo del patrón unidireccional:
                            // deshace de una vez toda la columna. Con el límite fijo de dos
                            // minutos que había antes, era el primero en agotarlo.
                            var moveBack = MotorMotion.MoveAndWait(
                                deviceconnect, chanelsDevice[2], (int)yReturn, stepRate, settleMs: 0);
                            if (!moveBack.Ok)
                                throw new Exception(
                                    $"Recorrido detenido al retornar el eje Y tras la columna {j + 1} " +
                                    $"de {grid.ImagesX}. {moveBack.Describe("Y")}");
                            travelY.Record(moveBack.PositionReached);
                        }
                        yColumnStart = yReturn;
                    }
                    else
                    {
                        // Las serpentinas empiezan la siguiente columna donde terminó esta.
                        yColumnStart = yColumnEnd;
                    }

                    Mat mosaicv = new Mat();
                    Cv2.VConcat(image, mosaicv);
                    finalimg[j] = mosaicv;

                    string mosaicpathv = Path.Combine(fullnamefolder, $"columnpic{j}.jpg");

                    mosaicv.SaveImage(mosaicpathv);
                    var namephoto1 = mosaicpathv.Split($"{Path.DirectorySeparatorChar}");
                    int lengtpicpath1 = namephoto1.Length;
                    var namepicstream1 = namephoto1[lengtpicpath1 - 1];
                    var urlstaticfiles1 = urlslocals[2] + $"/SouerceStaticFiles/{namefolder}/" + namepicstream1 + "?ranmd=" + rand.Next().ToString();
                    yield return urlstaticfiles1;
                }

                Cv2.HConcat(finalimg, mosaic);
                string mosaicpath = Path.Combine(fullnamefolder, $"HxV.jpg");
                mosaic.SaveImage(mosaicpath);
                var namephoto = mosaicpath.Split($"{Path.DirectorySeparatorChar}");
                int lengtpicpath = namephoto.Length;
                var namepicstream = namephoto[lengtpicpath - 1];

                var urlstaticfiles = urlslocals[2] + $"/SouerceStaticFiles/{namefolder}/" + namepicstream + "?ranmd=" + rand.Next().ToString();
                EndStatus("succes");
                tourCompleted = true;
                yield return urlstaticfiles;
            }
            finally
            {
                // Se ejecuta tanto si el recorrido terminó como si se cortó por un fallo o
                // porque el cliente dejó de consumir el streaming. Antes cada salida hacía
                // su propia limpieza a mano y la vía de error no marcaba el tour, que se
                // quedaba con el estado vacío: un recorrido cortado era indistinguible en
                // la base de datos de uno todavía en marcha.
                //
                // El estado se escribe ANTES de tocar el motor: devolver la platina al
                // origen puede llevar varios minutos, y si el proceso se cae durante esa
                // maniobra el recorrido volvería a quedar sin marcar, que es justo lo que
                // se está corrigiendo.
                if (!tourCompleted)
                {
                    try { EndStatus("error"); }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[TakeTour] No se pudo marcar el recorrido como fallido: {ex.Message}");
                    }
                }

                FinalizeTour(deviceconnect, travelX, travelY, stepRate);
            }
        }

        /// <summary>
        /// Cierre del recorrido: devuelve la platina al punto físico donde empezó, vuelve
        /// a sincronizar el contador con ese origen y suelta el dispositivo.
        ///
        /// Devolver la platina importa más de lo que parece. El contador y la posición
        /// real se separan durante el recorrido (ver <see cref="AxisTravel"/>), así que
        /// sin este cierre cada recorrido dejaba la platina en un sitio distinto del que
        /// decía el contador, y el error se acumulaba de un recorrido al siguiente.
        /// </summary>
        private static void FinalizeTour(
            KCubeInertialMotor device,
            AxisTravel travelX,
            AxisTravel travelY,
            long stepRate)
        {
            try
            {
                if (device.IsConnected)
                {
                    ReturnToPhysicalOrigin(device, InertialMotorStatus.MotorChannels.Channel1, travelX, stepRate, "X");
                    ReturnToPhysicalOrigin(device, InertialMotorStatus.MotorChannels.Channel2, travelY, stepRate, "Y");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TakeTour] No se pudo devolver la platina al origen: {ex.Message}");
            }
            finally
            {
                try { device.StopPolling(); } catch { }
                try { device.Disconnect(true); } catch { }
            }
        }

        /// <summary>
        /// Lleva un eje al punto físico donde empezó el recorrido y pone su contador a
        /// cero. El número de pasos del retorno no es el mismo que se recorrió a la ida,
        /// porque cada sentido rinde una distancia distinta por paso.
        /// </summary>
        private static void ReturnToPhysicalOrigin(
            KCubeInertialMotor device,
            InertialMotorStatus.MotorChannels channel,
            AxisTravel travel,
            long stepRate,
            string axisLabel)
        {
            int target = travel.CounterForPhysicalOrigin();

            if (target != travel.Counter)
            {
                Console.WriteLine($"[TakeTour] Devolviendo el eje {axisLabel} al origen: " +
                    $"desplazamiento físico acumulado {travel.PhysicalMm:F4} mm, " +
                    $"contador {travel.Counter} -> {target}.");

                var result = MotorMotion.MoveAndWait(device, channel, target, stepRate, settleMs: 0);
                if (!result.Ok)
                {
                    Console.WriteLine($"[TakeTour] {result.Describe(axisLabel)} " +
                        "El contador no se pone a cero: el siguiente recorrido tomará como origen " +
                        "el punto donde haya quedado la platina.");
                    return;
                }
                travel.Record(result.PositionReached);
            }

            try
            {
                device.SetPositionToZero(channel);
                Console.WriteLine($"[TakeTour] Eje {axisLabel} en su origen físico, contador a cero.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TakeTour] Eje {axisLabel}: no se pudo poner el contador a cero: {ex.Message}");
            }
        }
        public bool CreatesticherOpencv(int mode)/////// DEBE SER AISLADA EN SU PROPIA UNOT OF WORK
        {
            //var stitching = new emgu();

            var r1 = CreatesticherOpencv2(mode);
            var r2 = emgu.getstitcher(mode);
            bool resultado = r1 == r2 && r1 == true;
            return resultado;

        }


        ///<summary>
        ///Funcion de stitch encargada de procesar la imagenes definidas. 
        ///</summary>
        ///<return>
        ///bool return, on OK return true 
        ///</return>
        public bool CreatesticherOpencv2(int modes)/////// DEBE SER AISLADA EN SU PROPIA UNiT OF WORK
        {
            var carpetaPath = Path.Combine(Environment.CurrentDirectory, "StaticFiles", "datasetstitched");
            string[] archivos = Directory.GetFiles(carpetaPath, "*.jpg");
            Mat[] arraisMat = new Mat[archivos.Length];
            var output = new Mat();
            var outputarray1 = new Mat();
            int indexinter = 0;

            foreach (var archivo in archivos)
            {
                var img = new Mat(archivo.ToString());
                arraisMat[indexinter] = img;
                indexinter++;
            }

            var mode = modes == 0 ? OpenCvSharp.Stitcher.Mode.Scans : OpenCvSharp.Stitcher.Mode.Panorama;
            var stitched = Stitcher.Create(mode);
            var nuevo1 = new BestOf2NearestMatcher();

            var solucion = stitched.Stitch(arraisMat, output);
            var estado = solucion == Stitcher.Status.OK ? true : false;
            output.SaveImage(Path.Combine(Environment.CurrentDirectory, "StaticFiles", "openNative.jpg"));
            return estado;
        }
        ///<summary>
        ///Inicia la conexion con el dispositivo connectado 
        ///</summary>
        ///<return>
        ///Devuelve True si puede generar la lista de dispositivos connectados sin importar el tipo de dispositivo.
        ///</return>
        ///<param>
        ///Funcio sin argumentos
        ///</param>
        public bool builddeviceslist()
        {
            try
            {
                // Tell the device manager to get the list of all devices connected to the computer
                DeviceManagerCLI.BuildDeviceList();
                return true;
            }
            catch (Exception ex)
            {
                // An error occurred - see ex for details
                return false;

            }
        }
        ///<summary>
        ///se connecta para consultar los dispositivos del tipo Kim Connectados en la lista cargadas anteriormente en builddeviceslist()
        ///</summary>
        ///<return>
        ///Devuelve una lista de los dispositivos connectados.
        ///</return>
        ///<param>
        ///Funcio sin argumentos
        ///</param>
        public List<string> deviceslist()
        {
            if (builddeviceslist() == false) { return null; }
            return DeviceManagerCLI.GetDeviceList(KCubeInertialMotor.DevicePrefix_KIM101);
        }
        ///<summary>
        ///se crea el objeto que contiene el dispositivo que va a ser manejado en este caso y clase es un dispositi KIM04
        ///</summary>
        ///<return>
        ///retorna un device De tipo  KCubeInertialMotor o NULL si no se pudo crear el objeto
        ///</return>
        ///<param>
        ///nombre del dispositivo que se desea crear objeto para manejarlo
        ///</param>
        public async Task<KCubeInertialMotor> Getobjdevicekim(string devicename)
        {
            KCubeInertialMotor device = await Task.Run(() => KCubeInertialMotor.CreateKCubeInertialMotor(devicename));
            try
            {
                // Open a connection to the device.
                await Task.Run(() => device.Connect(devicename));
            }
            catch (Exception)
            {
                device.Disconnect();
                // Connection failed
                return null;
            }
            if (!device.IsSettingsInitialized())
            {
                try
                {
                    device.WaitForSettingsInitialized(1000);
                }
                catch (Exception)
                {
                    return null;
                }
            }
            return device;
        }

        ///<summary>
        ///metodo encargado del desplazamiento del motor
        ///</summary>
        ///<return>
        ///true para un movimiento correcto
        ///</return>
        ///<param>
        /// Device, canal a mover y posicion a mover.
        ///</param>

        /// <summary>
        /// Tamaño de paso medido para un eje, o null si ese eje no está caracterizado.
        /// Solo se acepta el valor cuando la medición de ambos sentidos está completa: un
        /// eje a medias daría un número peor que el nominal, no mejor.
        /// </summary>
        private static double? ReadMeasuredStepNm(
            GotsThorlabs.Database.EntityRepo.Entities.MotorCalibration? calibration,
            string axisName,
            bool forward)
        {
            var axis = calibration?.AxisStepCalibrations
                .FirstOrDefault(a => string.Equals(a.AxisName, axisName, StringComparison.OrdinalIgnoreCase));

            if (axis == null || axis.Status != "complete") return null;

            var raw = forward ? axis.StepSizeNmForward : axis.StepSizeNmBackward;
            return StepSizeCalculator.TryParse(raw, out var value) && value > 0
                ? value
                : null;
        }

        /// <summary>
        /// Movimiento con espera hasta que la platina llega. Se conserva por
        /// compatibilidad con quien lo llame desde fuera de esta clase; el recorrido usa
        /// <see cref="MotorMotion.MoveAndWait"/> directamente, porque necesita saber POR
        /// QUÉ falló un movimiento y un bool no lo permite: un límite de tiempo agotado,
        /// un eje trabado y una caída de comunicación llegaban aquí como el mismo
        /// "false". Al no recibir la velocidad configurada del grupo, esta versión asume
        /// la nominal, así que dimensiona el tiempo de forma más conservadora.
        /// </summary>
        public static bool Move_Method1(KCubeInertialMotor device, InertialMotorStatus.MotorChannels channel, int position)
        {
            var result = MotorMotion.MoveAndWait(device, channel, position, MotorMotion.DefaultStepRate);
            if (!result.Ok)
                Console.WriteLine($"[TakeTour] {result.Describe(channel.ToString())}");
            return result.Ok;
        }
        /// <summary>
        /// funcion encargada de crear la carpeta y la persistencia para la tabla tour
        /// </summary>
        /// <returns>retorna el nombre completo de la carpeta donde se va a guardar las imagenes </returns>
        /// <exception cref="NotImplementedException"></exception>
        public string CreateTour(string picsCalibrationId)
        {
            var currentPath = Directory.GetCurrentDirectory();
            string fullnamefolder = Path.Combine(currentPath, $"StaticFiles{Path.DirectorySeparatorChar}" + namefolder);
            bool status = Utilities.createFolder(fullnamefolder);

            //var allCalibrations = _db.GroupCalibrations.AsNoTracking().ToList();
            //using (var queryable = ConnectionSqlite.
            //CreateConnection())
            //{
            //    queryable.Open();
            //    string createTour = @$"INSERT INTO tour ( date,nameFolder, NumberX, NumberY, NumberZ, Camera) VALUES 
            //                        ( '{DateTime.Now.ToString()}', '{namefolder}', '{columns}', '{rows}', '0', '{indexCam}')";
            //    var rowsAffected = queryable.Query(createTour);
            //}

            var tour = new Tour
            {
                Date = DateTime.Now.ToString("o"),
                NameFolder = namefolder,
                NumberX = (long)(_areaX_mm * 1000m),
                NumberY = (long)(_areaY_mm * 1000m),
                NumberZ = 0,
                Camera = 0, // camera identified by LocalIdentifier; int index no longer used
                PicsCalibrationId = picsCalibrationId,
            };
            _db.Tours.Add(tour);
            _db.SaveChanges();
            _currentTourId = tour.IdTour;

            return fullnamefolder;

        }

        public void EndStatus(string statusOfTour)
        {
            //using (var queryable = ConnectionSqlite.CreateConnection())
            //{
            //    queryable.Open();
            //    string createTour = @$"UPDATE tour SET endStatus='{statusOfTour}' WHERE nameFolder = '{namefolder}'";
            //    var rowsAffected = queryable.Query(createTour);
            //}

            var tourToUpdate = _currentTourId.HasValue
                ? _db.Tours.FirstOrDefault(x => x.IdTour == _currentTourId.Value)
                : _db.Tours.FirstOrDefault(x => x.NameFolder == namefolder);

            if (tourToUpdate != null)
            {
                tourToUpdate.EndStatus = statusOfTour;
                _db.SaveChanges();
            }
            //throw new NotImplementedException();
        }

        public string TakeAPic(string nameFile, string path, int x, int y, int z)
        {
            Mat frame = new Mat();
            string pathsave;
            double focusScore;
            string nameimage;

            // capture frame via injected camera service resolved from GroupCalibration → Camera
            using (var captured = _cameraService?.CaptureFrame(localIdentifier))
            {
                // Clone the captured Mat so it remains valid after 'captured' is disposed
                var ownedFrame = (captured != null) ? captured.Clone() : new Mat();

                var frameheight = ownedFrame.Rows > 0 ? ownedFrame.Rows : 1080;
                var framewidth = ownedFrame.Cols > 0 ? ownedFrame.Cols : 1920;

                // Guard de última línea de defensa: MosaicGridCalculator.Calculate ya
                // valida el tamaño del grid antes de llegar aquí, pero se revalida con
                // el tamaño de frame real (puede diferir del usado en el cálculo del
                // grid si la cámara respondió distinto entre el probe y esta captura).
                // Sin este guard, un rows/columns inflado termina en OpenCvSharp
                // lanzando "Failed to allocate ... bytes" sin contexto de la causa.
                long mosaicBytesNeeded = (long)rows * frameheight * (long)columns * framewidth * 3L;
                if (mosaicBytesNeeded > MosaicGridCalculator.MaxMosaicBytes)
                {
                    throw new InvalidOperationException(
                        $"TakeAPic: el mosaico ({columns}x{rows} imágenes de {framewidth}x{frameheight}px) " +
                        $"pesaría ~{mosaicBytesNeeded / 1_000_000_000.0:F2} GB, por encima del límite de " +
                        $"seguridad ({MosaicGridCalculator.MaxMosaicBytes / 1_000_000_000.0:F1} GB). " +
                        $"Revise la calibración del grupo y el área solicitada antes de reintentar.");
                }

                using (var mosaic = new Mat(rows * frameheight, columns * framewidth, MatType.CV_8UC3))
                {
                    // Dispose any previous stored frame at this slot to avoid leaks
                    image[y]?.Dispose();
                    image[y] = ownedFrame;

                    // Nitidez de la imagen: varianza del Laplaciano normalizada.
                    //
                    // Antes aquí se calculaba el Laplaciano en 'shaperesult' y acto
                    // seguido se descartaba, porque MeanStdDev se aplicaba sobre
                    // ownedFrame (la imagen a color original). Lo que se guardaba en
                    // gausianVal era entonces la varianza de intensidad de la imagen
                    // cruda — una medida de contraste, no de enfoque. FocusMetrics mide
                    // sobre el Laplaciano, que es lo que corresponde.
                    //
                    // Se usa Sharpness (normalizada por región, escala y luminancia) y no
                    // la varianza cruda, para que el valor guardado aquí sea el mismo que
                    // muestra el streaming y el que se compara contra el umbral de la
                    // cámara. Con la cruda, tres sitios mostraban tres números distintos.
                    //
                    // Ojo: las filas de 'image' anteriores a cada uno de estos cambios
                    // tienen valores en otra escala y no son comparables con las nuevas.
                    focusScore = FocusMetrics.Sharpness(ownedFrame);

                    Rect region = new Rect(ownedFrame.Cols * x, ownedFrame.Rows * y, ownedFrame.Cols, ownedFrame.Rows);
                    ownedFrame.CopyTo(mosaic.SubMat(region));
                    nameimage = $"{nameFile}{x}_{y}.jpg";
                    pathsave = Path.Combine(path, nameimage);

                    ownedFrame.SaveImage(pathsave);
                }
            }

            //using (var queryable = ConnectionSqlite.CreateConnection())
            //{
            //    queryable.Open();
            //    string createTour = @$"INSERT INTO image ( name ,gausianVal, path, X, Y, Z,idTour)
            //                        SELECT '{nameimage}',{resultadolaplace}, '{namefolder}',{x},{y},0,idTour FROM tour WHERE nameFolder = '{namefolder}'";
            //    var rowsAffected = queryable.Query(createTour);
            //}

            var resolvedTourId = _currentTourId ?? _db.Tours
                .Where(x => x.NameFolder == namefolder)
                .Select(x => (long?)x.IdTour)
                .FirstOrDefault();

            if (resolvedTourId.HasValue)
            {
                _db.Images.Add(new GotsThorlabs.Database.EntityRepo.Entities.Image
                {
                    Name = nameimage,
                    // Se guarda el double directamente: antes se formateaba a string y
                    // se volvía a parsear, lo que dependía de la cultura del sistema
                    // (con coma decimal el parse fallaba y quedaba null).
                    GausianVal = focusScore,
                    Path = namefolder,
                    X = x,
                    Y = y,
                    Z = 0,
                    IdTour = resolvedTourId.Value
                });
                _db.SaveChanges();
            }

            return pathsave;
        }
    }
}
