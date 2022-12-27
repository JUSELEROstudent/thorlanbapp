using Microsoft.AspNetCore.SignalR;
using OpenCvSharp;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace GotsThorlabs.Hubs
{
    public class StreamingHub : Hub
    {
        public async IAsyncEnumerable<byte[]> Counter(
         int count,
         int delay,
         [EnumeratorCancellation]
        CancellationToken cancellationToken)
        {
            var idconection = Context.ConnectionAborted;
            Console.WriteLine(idconection);
            var acptationvalue = true;
            using var capture = new VideoCapture(0, VideoCaptureAPIs.DSHOW);
            //for (var i = 0; i < count; i++)
            while (acptationvalue)
            {
                
                if (!capture.IsOpened()) {
                    acptationvalue = false;
                    capture.FrameWidth = 1920;
                    capture.FrameHeight = 1080;
                    capture.AutoFocus = true;

                    const int sleepTime = 10;
                }

                //using var window = new Window("capture");
                var image = new Mat();

                capture.Read(image);
                string pathsave = string.Format("{0}\\camtaked.jpg", AppDomain.CurrentDomain.BaseDirectory);
                image.SaveImage(pathsave);
                var imgretonr =image.ToBytes();

                    // Hace falta agregar una capa de seguridad de acuerdo a los roles para poder ejecutar esta linea
                    cancellationToken.ThrowIfCancellationRequested();

                yield return imgretonr;

                // Use the cancellationToken in other APIs that accept cancellation
                // tokens so the cancellation can flow down to them.
                await Task.Delay(delay);// , cancellationToken);
            }
        }
    }

    public class resourcesignal { 
        public string Name { get; set; }
        // public CancellationToken cancelacion { get; set; }
        public int numero { get; set; }
        public Bitmap imagen { get; set; }
       
    }
}
