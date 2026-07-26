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

       
        public async IAsyncEnumerable<dynamic> Createmosaicstepbystep(decimal areaX_mm, decimal areaY_mm, string kimDeviceId, string groupCalibrationId)
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
            currentDeviceSettings.Drive.Channel(chanelsDevice[1]).StepRate = 200;
            currentDeviceSettings.Drive.Channel(chanelsDevice[1]).StepAcceleration = 100;
            currentDeviceSettings.Drive.Channel(chanelsDevice[2]).StepRate = 200;
            currentDeviceSettings.Drive.Channel(chanelsDevice[2]).StepAcceleration = 100;
            deviceconnect.SetSettings(currentDeviceSettings, true, true);



            Decimal newPos = deviceconnect.GetPosition(InertialMotorStatus.MotorChannels.Channel1);
            var rand = new Random();

            // SECCIÓN CALCULO DE GRID BASADO EN CALIBRACIÓN
            // Se obtienen todas las calibraciones del grupo y se calculan los parámetros
            // del grid (número de imágenes, espaciado del motor, overlap) en base al área
            // deseada (mm) y a la mejor calibración disponible por eje.
            // .Trim() defensivo (igual que se hace con 'device' más abajo): evita que un
            // espacio en blanco accidental en el valor recibido del cliente SignalR haga
            // que el filtro no matchee ningún registro.
            var groupCalibrationIdTrimmed = groupCalibrationId?.Trim() ?? groupCalibrationId;
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

            var grid = MosaicGridCalculator.Calculate(areaX_mm, areaY_mm, allCalibrations, frameWidth, frameHeight);

            Console.WriteLine($"[TakeTour] Grid calculado: ImagesX={grid.ImagesX}, ImagesY={grid.ImagesY}, " +
                $"frame={frameWidth}x{frameHeight}px, MotorStepX={grid.MotorStepX}, MotorStepY={grid.MotorStepY}, " +
                $"PxPerStepX={grid.PxPerStepX:G6}, PxPerStepY={grid.PxPerStepY:G6}, " +
                $"StepMmX={grid.StepMmX:G6}, StepMmY={grid.StepMmY:G6}.");

            // Inicializar arrays dinámicamente con el grid calculado.
            // columns = eje X (Channel1), rows = eje Y (Channel2)
            columns = grid.ImagesX;
            rows = grid.ImagesY;
            image = new Mat[rows];
            finalimg = new Mat[grid.ImagesX];
            mosaic = new Mat();
            // fin inicialización grid

            string fullnamefolder = CreateTour(grid.CalibrationX.PicsCalibrationId);

            // RECORRIDO EN PATRÓN "S":
            // - El eje X (Channel1) siempre avanza (j * MotorStepX).
            // - El eje Y (Channel2) alterna dirección por columna:
            //      columnas pares   → sube  (i_motor: 0 → ImagesY-1)
            //      columnas impares → baja  (i_motor: ImagesY-1 → 0)
            //   El almacenamiento en image[] siempre usa el índice espacial k
            //   para que VConcat preserve el orden espacial correcto.
            for (int j = 0; j < grid.ImagesX; j++)
            {
                bool estatusMovementA = Move_Method1(deviceconnect, chanelsDevice[1], j * grid.MotorStepX);
                if (!estatusMovementA)
                {
                    deviceconnect.StopPolling();
                    deviceconnect.Disconnect(true);
                    throw new Exception("Error al mover el dispositivo (eje X), revisar la coneccion [ES]");
                }

                for (int k = 0; k < grid.ImagesY; k++)
                {
                    // Índice lógico del motor en Y según el patrón S
                    int i_motor = (j % 2 == 0) ? k : (grid.ImagesY - 1 - k);

                    Mat frame = new Mat();

                    bool estatusMovement = Move_Method1(deviceconnect, chanelsDevice[2], i_motor * grid.MotorStepY);
                    if (!estatusMovement)
                    {
                        deviceconnect.StopPolling();
                        deviceconnect.Disconnect(true);
                        throw new Exception("Error al mover el dispositivo (eje Y), revisar la coneccion [ES]");
                    }
                    // Tomar imagen guardando en el índice espacial k (orden vertical correcto)
                    string pathsave = TakeAPic("unitofpics", fullnamefolder, j, k, 0);
                    var splitpathdir = pathsave.Split($"{Path.DirectorySeparatorChar}");
                    int dimpath = splitpathdir.Length;
                    var namephotounits = splitpathdir[dimpath - 1];
                    var urlunitpi = urlslocals[2] + $"/SouerceStaticFiles/{namefolder}/" + namephotounits + "?ranmd=" + rand.Next().ToString();
                    yield return urlunitpi;
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

            // Tidy up and exit
            deviceconnect.StopPolling();
            deviceconnect.Disconnect(true);
            var urlstaticfiles = urlslocals[2] + $"/SouerceStaticFiles/{namefolder}/" + namepicstream + "?ranmd=" + rand.Next().ToString();
            EndStatus("succes");
            yield return urlstaticfiles;

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

        public static bool Move_Method1(KCubeInertialMotor device, InertialMotorStatus.MotorChannels channel, int position)
        {
            // se crea la condicion para que la posicion a mover no sea igual a la acutal
            if (device.GetPosition(channel) == position) { return true; }
            try
            {
                // timeout 0 = no bloqueante: MoveTo retorna de inmediato, antes de que
                // la platina termine de moverse. El motor inercial avanza aplicando
                // pulsos de vibración y el contador de posición solo sube a medida que
                // se mueve físicamente, así que hay que esperar a que GetPosition()
                // reporte la posición pedida (y dejar que amortigüe la vibración
                // residual) antes de dejar tomar la foto. Antes esta función retornaba
                // "true" apenas se enviaba el comando, sin esperar nada, por lo que
                // TakeAPic podía capturar con la platina todavía en movimiento —
                // mismo patrón que ya se corrigió para la calibración automática en
                // PicsCalibrationService.MoveMotor, replicado aquí para la toma real.
                device.MoveTo(channel, position, 0);

                const int pollIntervalMs = 100;
                const int settleMs = 500;        // deja amortiguar la vibración residual tras llegar
                const int safetyMaxMs = 120_000; // 2 min, válvula de seguridad ante un motor trabado

                var elapsed = 0;
                while (device.GetPosition(channel) != position)
                {
                    Thread.Sleep(pollIntervalMs);
                    elapsed += pollIntervalMs;
                    if (elapsed >= safetyMaxMs)
                    {
                        // El motor no llegó a la posición esperada dentro del tiempo de
                        // seguridad: se reporta como movimiento fallido (igual que antes
                        // cuando MoveTo lanzaba excepción) en vez de tomar la foto a ciegas.
                        return false;
                    }
                }

                Thread.Sleep(settleMs);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
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
            string resultadolaplace;
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

                    // se hcae el calculo de la place para el estado del blur 
                    using var grayresult = new Mat();
                    using var shaperesult = new Mat();
                    Cv2.CvtColor(ownedFrame, grayresult, ColorConversionCodes.BGR2GRAY);
                    Cv2.Laplacian(grayresult, shaperesult, MatType.CV_64F);
                    Cv2.MeanStdDev(ownedFrame, out var mean, out var stddev);
                    resultadolaplace = (stddev.Val0 * stddev.Val0).ToString();

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
                    GausianVal = double.TryParse(resultadolaplace, out var blurValue) ? blurValue : null,
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
