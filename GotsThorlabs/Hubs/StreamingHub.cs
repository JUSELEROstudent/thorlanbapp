using Microsoft.AspNetCore.SignalR;
using System.Runtime.CompilerServices;

namespace GotsThorlabs.Hubs
{
    public class StreamingHub : Hub
    {
        public async IAsyncEnumerable<resourcesignal> Counter(
         int count,
         int delay,
         [EnumeratorCancellation]
        CancellationToken cancellationToken)
        {
            for (var i = 0; i < count; i++)
            {
                var resueta = new resourcesignal
                {
                    Name = "Sebastian",
                    numero = i
                };
                // Check the cancellation token regularly so that the server will stop
                // producing items if the client disconnects.
                cancellationToken.ThrowIfCancellationRequested();

                yield return resueta;

                // Use the cancellationToken in other APIs that accept cancellation
                // tokens so the cancellation can flow down to them.
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    public class resourcesignal { 
        public string Name { get; set; }
        // public CancellationToken cancelacion { get; set; }
        public int numero { get; set; }
    }
}
