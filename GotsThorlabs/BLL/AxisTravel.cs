namespace GotsThorlabs.BLL
{
    /// <summary>
    /// Lleva la cuenta de dónde está físicamente un eje, que no es lo mismo que el
    /// contador de pasos del KIM101.
    ///
    /// El actuador inercial avanza distinto según el sentido (en el montaje medido la
    /// vuelta rinde alrededor de un 28 % menos que la ida). Para que una columna de
    /// vuelta cubra la misma distancia que la de ida hay que comandarle MÁS pasos, así
    /// que al cabo de un par de columnas la platina ha vuelto a su sitio pero el
    /// contador ha bajado. Repetido a lo largo de un recorrido, el contador se aleja
    /// del origen físico sin límite: tras seis columnas del recorrido medido acumulaba
    /// unos -97.000 pasos, casi 2 mm de desfase entre lo que decía el contador y dónde
    /// estaba realmente la platina.
    ///
    /// Eso convertía el "volver a la posición 0" del siguiente recorrido en dos
    /// problemas a la vez: un movimiento larguísimo que agotaba el límite de tiempo, y
    /// —si llegaba a completarse— un desplazamiento a un punto que ya no era el origen
    /// físico. Esta clase mantiene ambas magnitudes separadas para poder deshacer el
    /// desplazamiento real en vez del contable.
    /// </summary>
    public sealed class AxisTravel
    {
        /// <summary>
        /// Pasos mínimos que debe tener la corrección de cierre para que valga la pena
        /// comandarla. Por debajo de esto el remedio es peor que la desviación.
        /// </summary>
        public const int MinCorrectionSteps = 2;

        /// <summary>Nanómetros que avanza cada paso en sentido positivo.</summary>
        public double NmForward { get; }

        /// <summary>Nanómetros que avanza cada paso en sentido negativo.</summary>
        public double NmBackward { get; }

        /// <summary>Valor actual del contador del dispositivo.</summary>
        public int Counter { get; private set; }

        /// <summary>
        /// Desplazamiento físico acumulado desde el origen del recorrido, en nanómetros.
        /// </summary>
        public double PhysicalNm { get; private set; }

        public AxisTravel(double nmForward, double nmBackward, int startCounter = 0)
        {
            // Un valor no medido o absurdo haría que la corrección de origen fuese peor
            // que no hacer nada: se cae al otro sentido, y si tampoco hay, a simétrico.
            NmForward = nmForward > 0 ? nmForward : (nmBackward > 0 ? nmBackward : MosaicGridCalculator.NmPerStep);
            NmBackward = nmBackward > 0 ? nmBackward : NmForward;
            Counter = startCounter;
        }

        /// <summary>
        /// Registra que el contador llegó a <paramref name="newCounter"/>, convirtiendo
        /// los pasos recorridos a distancia física con el tamaño de paso del sentido en
        /// que se movió.
        /// </summary>
        public void Record(int newCounter)
        {
            long delta = (long)newCounter - Counter;
            PhysicalNm += delta >= 0 ? delta * NmForward : delta * NmBackward;
            Counter = newCounter;
        }

        /// <summary>
        /// Posición de contador a la que hay que ir para que la platina vuelva al punto
        /// físico donde empezó el recorrido. Como el rendimiento depende del sentido, el
        /// número de pasos del retorno no coincide con el que se recorrió a la ida.
        /// </summary>
        public int CounterForPhysicalOrigin()
        {
            // Si estamos por delante hay que retroceder, y el retroceso rinde menos:
            // hacen falta más pasos de los que se dieron para llegar hasta aquí.
            double stepNm = PhysicalNm > 0 ? NmBackward : NmForward;
            long steps = (long)Math.Round(-(PhysicalNm / stepNm));

            // Banda muerta. Redondear a entero los pasos de cada columna deja al cerrar
            // el recorrido un residuo de unas pocas decenas de nanómetros, y corregirlo
            // exigiría comandar uno o dos pasos sueltos: el movimiento menos repetible que
            // hace un actuador inercial —en la calibración un paso aislado da
            // desplazamientos erráticos, mientras que mil dan una recta limpia—, capaz de
            // dejar el eje pasado hacia el otro lado. Y no compensa el riesgo: con esta
            // óptica un píxel son varios cientos de nanómetros, así que ese residuo es una
            // fracción pequeña de píxel y no cambia nada de lo que se ve.
            if (Math.Abs(steps) < MinCorrectionSteps) return Counter;

            long target = Counter + steps;
            return (int)Math.Clamp(target, int.MinValue, int.MaxValue);
        }

        /// <summary>Desplazamiento físico acumulado en milímetros, para los mensajes.</summary>
        public double PhysicalMm => PhysicalNm / 1_000_000.0;
    }
}
