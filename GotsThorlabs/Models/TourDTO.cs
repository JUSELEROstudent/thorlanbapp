namespace GotsThorlabs.Models
{
    public class TourDTO
    {
        public long IdTour { get; set; }
        public string Date { get; set; } = default!;
        public string NameFolder { get; set; } = default!;
        public long NumberX { get; set; }
        public long NumberY { get; set; }
        public long NumberZ { get; set; }
        public long Camera { get; set; }
        public string? EndStatus { get; set; }
        public string PicsCalibrationId { get; set; } = default!;
    }

    /// <summary>
    /// Un recorrido, indicando además si su stitching ya está hecho y disponible.
    ///
    /// El resultado del stitching no se guarda en la base: es un archivo de nombre fijo
    /// dentro de la carpeta del recorrido (StaticFiles/{NameFolder}/openNative.jpg, ver
    /// ProcessTourData). Hasta ahora la única forma de saber si existía era pedir la
    /// imagen y ver si daba 404, así que la vista tenía que provocar un error para
    /// enterarse — y para elegir "el último recorrido con stitching" habría que sondear
    /// uno por uno. Comprobarlo aquí con un File.Exists lo resuelve en la misma consulta.
    /// </summary>
    public class TourResponseDTO
    {
        public long IdTour { get; set; }
        public string Date { get; set; } = default!;
        public string NameFolder { get; set; } = default!;
        public long NumberX { get; set; }
        public long NumberY { get; set; }
        public long NumberZ { get; set; }
        public long Camera { get; set; }
        public string? EndStatus { get; set; }
        public string PicsCalibrationId { get; set; } = default!;

        /// <summary>True cuando el mosaico ya está generado en disco.</summary>
        public bool HasStitching { get; set; }

        /// <summary>
        /// Ruta relativa desde la que se sirve el mosaico, o null si todavía no existe.
        /// Es relativa a propósito: el host y el puerto los pone el cliente, que ya sabe
        /// a qué backend está hablando.
        /// </summary>
        public string? StitchingUrl { get; set; }
    }
}
