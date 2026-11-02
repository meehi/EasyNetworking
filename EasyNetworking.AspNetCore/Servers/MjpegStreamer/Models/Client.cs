using System.Threading.Channels;

namespace EasyNetworking.AspNetCore.Servers.MjpegStreamer.Models
{
    internal sealed class Client(string id, Stream responseStream, CancellationToken cancellationToken)
    {
        public string Id { get; } = id;
        public Stream ResponseStream { get; } = responseStream;
        public CancellationToken CancellationToken { get; } = cancellationToken;

        public Channel<byte[]> FrameChannel { get; } =
            Channel.CreateBounded<byte[]>(new BoundedChannelOptions(1)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.DropOldest
            });
    }
}