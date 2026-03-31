using Microsoft.AspNetCore.Http;
using System.Threading.Channels;

namespace EasyNetworking.AspNetCore.Servers.MjpegStreamer.Models
{
    internal class Client(string id, HttpContext context)
    {
        public string Id { get; } = id;
        public HttpContext Context { get; } = context;
        public Channel<byte[]> FrameChannel { get; } = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
    }
}