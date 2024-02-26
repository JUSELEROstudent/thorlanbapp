namespace GotsThorlabs.Models
{
    public class Stitching
    {
        public int IdStitching { get; set; }
        public string Path { get; set; }
        public int IdImgResult { get; set; }
        public string TypeStitching { get; set; }
        public string Status { get; set; }
        public int IdTour { get; set; }

        // Constructor vacío
        public Stitching()
        {
        }

        // Constructor con parámetros
        public Stitching(int idStitching, string path, int idImgResult, string typeStitching, string status, int idTour)
        {
            IdStitching = idStitching;
            Path = path;
            IdImgResult = idImgResult;
            TypeStitching = typeStitching;
            Status = status;
            IdTour = idTour;
        }

        // Override del método ToString para mostrar información del objeto
        public override string ToString()
        {
            return $"IdStitching: {IdStitching}, Path: {Path}, IdImgResult: {IdImgResult}, TypeStitching: {TypeStitching}, Status: {Status}, IdTour: {IdTour}";
        }
    }

}
