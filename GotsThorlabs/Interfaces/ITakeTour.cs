using OpenCvSharp;

namespace GotsThorlabs.Interfaces
{
    public interface ITakeTour
    {
        public string CreateTour(string picsCalibrationId);

        public void EndStatus(string statusOfTour);

        public string TakeAPic(string nameFile, string path,int x,int y,int z);
        public IAsyncEnumerable<dynamic> Createmosaicstepbystep(decimal areaX_mm, decimal areaY_mm, string kimDeviceId, string groupCalibrationId);
    }
}