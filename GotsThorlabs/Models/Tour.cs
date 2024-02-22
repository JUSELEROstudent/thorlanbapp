﻿namespace GotsThorlabs.Models
{
    public class Tour
    {
        public int IdTour { get; set; }
        public string Date { get; set; }
        public int NumberX { get; set; }
        public int NumberY { get; set; }
        public int NumberZ { get; set; }
        public string Camera { get; set; }
        public string EndStatus { get; set; }
        public int IdStitching { get; set; }
        public int IdUser { get; set; }

        public Tour(int idTour, string date, int numberX, int numberY, int numberZ, string camera, string endStatus, int idStitching, int idUser)
        {
            IdTour = idTour;
            Date = date;
            NumberX = numberX;
            NumberY = numberY;
            NumberZ = numberZ;
            Camera = camera;
            EndStatus = endStatus;
            IdStitching = idStitching;
            IdUser = idUser;
        }

        public override string ToString()
        {
            return $"Tour(IdTour={IdTour}, Date='{Date}', NumberX={NumberX}, NumberY={NumberY}, NumberZ={NumberZ}, Camera='{Camera}', EndStatus='{EndStatus}', IdStitching={IdStitching}, IdUser={IdUser})";
        }

    }
}
