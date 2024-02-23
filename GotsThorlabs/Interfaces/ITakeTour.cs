using OpenCvSharp;

namespace GotsThorlabs.Interfaces
{
    public interface ITakeTour
    {
        public string CreateTour();

        public void EndStatus(string statusOfTour);

        public string TakeAPic(string nameFile, string path,int x,int y,int z);
        public IAsyncEnumerable<dynamic> Createmosaicstepbystep( int dimMove, string kimDeviceId);
    }
}
