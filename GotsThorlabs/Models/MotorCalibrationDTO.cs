namespace GotsThorlabs.Models
{
    /// <summary>Cuerpo para iniciar una caracterización mecánica.</summary>
    public class MotorCalibrationCreateDTO
    {
        public string GroupCailbrationId { get; set; } = default!;
        public string KimDeviceId { get; set; } = default!;

        /// <summary>
        /// Parámetros con los que se va a medir. Son los mismos que después usarán la
        /// calibración óptica y el recorrido: medir a una velocidad y moverse a otra
        /// invalidaría el resultado.
        /// </summary>
        public long StepRate { get; set; } = 200;

        public long StepAcceleration { get; set; } = 100;

        public string? AditionalInfo { get; set; }
    }

    /// <summary>Cuerpo para registrar la lectura del pie de rey en una parada.</summary>
    public class AxisStepMeasurementCreateDTO
    {
        /// <summary>"x" o "y".</summary>
        public string AxisName { get; set; } = default!;

        /// <summary>"forward" o "backward".</summary>
        public string Direction { get; set; } = default!;

        /// <summary>Orden de la parada. 0 es la lectura de partida.</summary>
        public long Sequence { get; set; }

        /// <summary>Pasos acumulados desde el inicio de la travesía.</summary>
        public long StepsCommanded { get; set; }

        /// <summary>Lectura del instrumento en milímetros.</summary>
        public double CaliperReadingMm { get; set; }
    }

    /// <summary>Cuerpo para corregir una lectura ya registrada.</summary>
    public class AxisStepMeasurementUpdateDTO
    {
        public double CaliperReadingMm { get; set; }
    }

    /// <summary>Cuerpo para mover el motor entre paradas.</summary>
    public class MotorMoveDTO
    {
        public string AxisName { get; set; } = default!;

        /// <summary>Posición absoluta objetivo, en pasos.</summary>
        public int TargetSteps { get; set; }
    }

    public class AxisStepMeasurementDTO
    {
        public string AxisStepMeasurementId { get; set; } = default!;
        public string Direction { get; set; } = default!;
        public long Sequence { get; set; }
        public long StepsCommanded { get; set; }
        public string CaliperReadingMm { get; set; } = default!;
        public string MeasuredAt { get; set; } = default!;

        /// <summary>Desplazamiento respecto a la lectura de partida, en milímetros.</summary>
        public string? DisplacementMm { get; set; }
    }

    public class AxisStepCalibrationDTO
    {
        public string AxisStepCalibrationId { get; set; } = default!;
        public string AxisName { get; set; } = default!;
        public string? StepSizeNmForward { get; set; }
        public string? StepSizeNmBackward { get; set; }
        public string? StepSizeNm { get; set; }
        public string? HysteresisPct { get; set; }
        public string? RelativeErrorPct { get; set; }
        public string Status { get; set; } = default!;

        /// <summary>
        /// True cuando la histéresis supera el umbral de aviso. No impide operar: solo
        /// señala que el patrón de recorrido en "S" merece revisión.
        /// </summary>
        public bool HysteresisWarning { get; set; }

        public List<AxisStepMeasurementDTO> Measurements { get; set; } = new();
    }

    public class MotorCalibrationDTO
    {
        public string MotorCalibrationId { get; set; } = default!;
        public string GroupCailbrationId { get; set; } = default!;
        public string KimDeviceId { get; set; } = default!;
        public long StepRate { get; set; }
        public long StepAcceleration { get; set; }
        public string Status { get; set; } = default!;
        public long Acepted { get; set; }
        public string Date { get; set; } = default!;
        public string? AditionalInfo { get; set; }
        public List<AxisStepCalibrationDTO> Axes { get; set; } = new();
    }
}
