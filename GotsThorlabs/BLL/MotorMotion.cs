using System.Diagnostics;
using Thorlabs.MotionControl.KCube.InertialMotorCLI;

namespace GotsThorlabs.BLL
{
    /// <summary>Cómo terminó un movimiento comandado al motor.</summary>
    public enum MoveStatus
    {
        /// <summary>El contador llegó exactamente a la posición pedida.</summary>
        Success,

        /// <summary>Se agotó el presupuesto de tiempo calculado para la distancia.</summary>
        Timeout,

        /// <summary>El motor dejó de avanzar: el contador no cambió durante la ventana de estancamiento.</summary>
        Stalled,

        /// <summary>La API del dispositivo lanzó una excepción (comunicación, estado inválido...).</summary>
        Faulted,

        /// <summary>El llamador canceló la operación.</summary>
        Cancelled
    }

    /// <summary>
    /// Resultado detallado de un movimiento. Se devuelve en vez de un bool porque
    /// las cuatro formas de fallar necesitan acciones distintas de quien llama, y
    /// antes las cuatro llegaban como el mismo "false" con el mensaje genérico
    /// "revisar la conexión": un timeout, un eje trabado y una caída de USB eran
    /// indistinguibles en el log.
    /// </summary>
    public sealed class MoveResult
    {
        public MoveStatus Status { get; init; }

        /// <summary>Posición que se pidió.</summary>
        public int Target { get; init; }

        /// <summary>Posición en la que quedó realmente el contador.</summary>
        public int PositionReached { get; init; }

        /// <summary>Posición desde la que arrancó el movimiento.</summary>
        public int PositionStart { get; init; }

        public double ElapsedSeconds { get; init; }

        /// <summary>Presupuesto de tiempo que se calculó para esta distancia, en segundos.</summary>
        public double BudgetSeconds { get; init; }

        public string? Error { get; init; }

        public bool Ok => Status == MoveStatus.Success;

        /// <summary>Pasos que faltaron para llegar.</summary>
        public long StepsRemaining => Math.Abs((long)Target - PositionReached);

        /// <summary>Pasos que sí se recorrieron.</summary>
        public long StepsAdvanced => Math.Abs((long)PositionReached - PositionStart);

        /// <summary>
        /// Mensaje pensado para que quien lo lea sepa qué revisar sin tener que
        /// reproducir el fallo: incluye el eje, a dónde iba, hasta dónde llegó y
        /// cuánto tardó.
        /// </summary>
        public string Describe(string axisLabel) => Status switch
        {
            MoveStatus.Success =>
                $"Eje {axisLabel}: movimiento a {Target} completado en {ElapsedSeconds:F1}s.",

            MoveStatus.Timeout =>
                $"Eje {axisLabel}: se agotó el tiempo yendo a {Target}. Salió de {PositionStart}, " +
                $"llegó a {PositionReached} ({StepsAdvanced} de {Math.Abs((long)Target - PositionStart)} pasos) " +
                $"en {ElapsedSeconds:F1}s, con un presupuesto de {BudgetSeconds:F0}s. " +
                $"El motor avanzaba pero más lento de lo previsto: revise StepRate y la carga mecánica del eje.",

            MoveStatus.Stalled =>
                $"Eje {axisLabel}: el motor dejó de avanzar yendo a {Target}. Se quedó en {PositionReached} " +
                $"tras {ElapsedSeconds:F1}s con {StepsRemaining} pasos pendientes. " +
                $"Es el patrón de un eje trabado o en el final de su recorrido, no de un problema de tiempo.",

            MoveStatus.Faulted =>
                $"Eje {axisLabel}: el dispositivo falló yendo a {Target} (posición {PositionReached}): {Error}",

            MoveStatus.Cancelled =>
                $"Eje {axisLabel}: movimiento a {Target} cancelado en {PositionReached}.",

            _ => $"Eje {axisLabel}: estado desconocido."
        };
    }

    /// <summary>
    /// Movimiento del KIM101 con espera activa hasta que el contador llega.
    ///
    /// Existe porque la misma rutina estaba copiada en tres sitios (el recorrido, la
    /// calibración óptica y la caracterización mecánica) y las tres tenían límites de
    /// tiempo distintos elegidos a ojo. El del recorrido —dos minutos fijos— era el
    /// problema: los movimientos reales de un tour son de decenas de miles de pasos y
    /// a unos cientos de pasos por segundo eso son varios minutos, así que el
    /// recorrido se cortaba solo por haber pedido un desplazamiento largo.
    ///
    /// La corrección no es subir la constante, sino dejar de usar una constante: el
    /// presupuesto se calcula a partir de la distancia y de la velocidad configurada.
    /// Quien detecta de verdad los fallos es la ventana de estancamiento, que salta en
    /// segundos cuando el eje no avanza en lugar de esperar a que expire el límite.
    /// </summary>
    public static class MotorMotion
    {
        /// <summary>Cada cuánto se consulta la posición mientras se espera.</summary>
        public const int PollIntervalMs = 100;

        /// <summary>
        /// Espera tras llegar, para que amortigüe la vibración residual del actuador
        /// inercial antes de capturar. Sin esto la foto sale movida.
        /// </summary>
        public const int SettleMs = 500;

        /// <summary>
        /// Cuánto se multiplica el tiempo teórico del movimiento. Cubre la rampa de
        /// aceleración, la granularidad del sondeo del dispositivo y que el motor
        /// rinda algo menos que su velocidad nominal bajo carga.
        /// </summary>
        public const double TimeoutSafetyFactor = 3.0;

        /// <summary>Margen fijo que se suma al presupuesto, para los movimientos cortos.</summary>
        public const int TimeoutMarginMs = 15_000;

        /// <summary>Presupuesto mínimo: ningún movimiento se juzga en menos de esto.</summary>
        public const int MinTimeoutMs = 30_000;

        /// <summary>
        /// Tope absoluto. No es un límite esperado sino una última barrera para que un
        /// fallo raro no deje el recorrido colgado indefinidamente.
        /// </summary>
        public const int MaxTimeoutMs = 1_800_000; // 30 min

        /// <summary>
        /// Tiempo sin que el contador cambie que se considera motor detenido. Este es
        /// el detector de fallos útil: un eje trabado se delata en segundos, sin
        /// consumir el presupuesto completo del movimiento.
        /// </summary>
        public const int StallTimeoutMs = 20_000;

        /// <summary>Velocidad asumida cuando el grupo no tiene caracterización mecánica.</summary>
        public const long DefaultStepRate = 200;

        /// <summary>
        /// Presupuesto de tiempo para recorrer <paramref name="deltaSteps"/> pasos a
        /// <paramref name="stepRate"/> pasos por segundo.
        /// </summary>
        public static int TimeoutForMs(long deltaSteps, long stepRate)
        {
            var rate = Math.Max(stepRate, 1);
            double nominalMs = Math.Abs(deltaSteps) / (double)rate * 1000.0;
            double budget = nominalMs * TimeoutSafetyFactor + TimeoutMarginMs;
            return (int)Math.Clamp(budget, MinTimeoutMs, MaxTimeoutMs);
        }

        /// <summary>
        /// Comanda un movimiento absoluto y espera a que el contador llegue.
        ///
        /// El tiempo se mide con reloj real. La versión anterior sumaba el intervalo de
        /// sondeo en cada vuelta e ignoraba lo que costaba consultar la posición, así
        /// que el límite nominal de dos minutos correspondía a un tiempo real distinto
        /// —y variable— en cada ejecución.
        /// </summary>
        /// <param name="settleMs">
        /// Espera de amortiguación tras llegar. Se puede poner a 0 en movimientos de
        /// reposicionamiento donde después no se captura ninguna imagen.
        /// </param>
        public static MoveResult MoveAndWait(
            KCubeInertialMotor device,
            InertialMotorStatus.MotorChannels channel,
            int position,
            long stepRate,
            CancellationToken ct = default,
            int settleMs = SettleMs)
        {
            int start;
            try
            {
                start = device.GetPosition(channel);
            }
            catch (Exception ex)
            {
                return new MoveResult
                {
                    Status = MoveStatus.Faulted,
                    Target = position,
                    Error = $"no se pudo leer la posición inicial: {ex.Message}"
                };
            }

            if (start == position)
            {
                return new MoveResult
                {
                    Status = MoveStatus.Success,
                    Target = position,
                    PositionStart = start,
                    PositionReached = start
                };
            }

            long delta = Math.Abs((long)position - start);
            int timeoutMs = TimeoutForMs(delta, stepRate);
            double budgetSeconds = timeoutMs / 1000.0;

            var sw = Stopwatch.StartNew();
            int lastPosition = start;
            double lastProgressSeconds = 0;

            try
            {
                // timeout 0 = no bloqueante: MoveTo vuelve de inmediato y el contador
                // sube a medida que la platina se mueve de verdad, así que la espera
                // la hacemos nosotros consultando la posición.
                device.MoveTo(channel, position, 0);

                while (true)
                {
                    int current = device.GetPosition(channel);
                    if (current == position) break;

                    double now = sw.Elapsed.TotalSeconds;

                    if (current != lastPosition)
                    {
                        lastPosition = current;
                        lastProgressSeconds = now;
                    }
                    else if ((now - lastProgressSeconds) * 1000.0 >= StallTimeoutMs)
                    {
                        StopQuietly(device, channel);
                        return new MoveResult
                        {
                            Status = MoveStatus.Stalled,
                            Target = position,
                            PositionStart = start,
                            PositionReached = current,
                            ElapsedSeconds = now,
                            BudgetSeconds = budgetSeconds
                        };
                    }

                    if (now * 1000.0 >= timeoutMs)
                    {
                        StopQuietly(device, channel);
                        return new MoveResult
                        {
                            Status = MoveStatus.Timeout,
                            Target = position,
                            PositionStart = start,
                            PositionReached = current,
                            ElapsedSeconds = now,
                            BudgetSeconds = budgetSeconds
                        };
                    }

                    if (ct.IsCancellationRequested)
                    {
                        StopQuietly(device, channel);
                        return new MoveResult
                        {
                            Status = MoveStatus.Cancelled,
                            Target = position,
                            PositionStart = start,
                            PositionReached = current,
                            ElapsedSeconds = now,
                            BudgetSeconds = budgetSeconds
                        };
                    }

                    Thread.Sleep(PollIntervalMs);
                }
            }
            catch (Exception ex)
            {
                StopQuietly(device, channel);
                return new MoveResult
                {
                    Status = MoveStatus.Faulted,
                    Target = position,
                    PositionStart = start,
                    PositionReached = lastPosition,
                    ElapsedSeconds = sw.Elapsed.TotalSeconds,
                    BudgetSeconds = budgetSeconds,
                    Error = ex.Message
                };
            }

            if (settleMs > 0) Thread.Sleep(settleMs);

            return new MoveResult
            {
                Status = MoveStatus.Success,
                Target = position,
                PositionStart = start,
                PositionReached = position,
                ElapsedSeconds = sw.Elapsed.TotalSeconds,
                BudgetSeconds = budgetSeconds
            };
        }

        /// <summary>
        /// Detiene el canal tras un fallo. Sin esto el dispositivo sigue intentando
        /// alcanzar el objetivo después de que el programa ha dado el movimiento por
        /// perdido, y la platina se mueve sola mientras se cierra el recorrido.
        /// </summary>
        private static void StopQuietly(KCubeInertialMotor device, InertialMotorStatus.MotorChannels channel)
        {
            try { device.Stop(channel); }
            catch { /* el motor ya puede estar caído; el fallo real ya se está reportando */ }
        }
    }
}
