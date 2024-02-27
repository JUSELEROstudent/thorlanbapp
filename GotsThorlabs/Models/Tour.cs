using GotsThorlabs.Services;
using Dapper;

namespace GotsThorlabs.Models
{
    public class Tour
    {
        public int IdTour { get; set; }
        public string Date { get; set; }
        public string NameFolder { get; set; }
        public int NumberX { get; set; }
        public int NumberY { get; set; }
        public int NumberZ { get; set; }
        public string Camera { get; set; }
        public string EndStatus { get; set; }
        public int? IdStitching { get; set; }
        public int? IdUser { get; set; }

        //public Tour(int idTour, string date, string namefolder, int numberX, int numberY, int numberZ, string camera, string endStatus, int? idStitching, int? idUser)
        //{
        //    IdTour = idTour;
        //    Date = date;
        //    NameFolder = namefolder;
        //    NumberX = numberX;
        //    NumberY = numberY;
        //    NumberZ = numberZ;
        //    Camera = camera;
        //    EndStatus = endStatus;
        //    IdStitching = idStitching;
        //    IdUser = idUser;
        //}

        public override string ToString()
        {
            return $"Tour(IdTour={IdTour}, Date='{Date}', NumberX={NumberX}, NumberY={NumberY}, NumberZ={NumberZ}, Camera='{Camera}', EndStatus='{EndStatus}', IdStitching={IdStitching}, IdUser={IdUser})";
        }

        public static IEnumerable<Tour> GetTours()
        {
            using (var queriable= ConnectionSqlite.CreateConnection())
            {
                string sql = "SELECT * FROM Tour";
                var items = queriable.Query<Tour>(sql);
                return items;

            }
        }
        public static Tour GetToursById(int idTour)
        {
            using (var queriable = ConnectionSqlite.CreateConnection())
            {
                string sql = $"SELECT * FROM Tour  WHERE idTour = {idTour}";
                var items = queriable.Query<Tour>(sql);
                return items.First();

            }
        }
    }
}
