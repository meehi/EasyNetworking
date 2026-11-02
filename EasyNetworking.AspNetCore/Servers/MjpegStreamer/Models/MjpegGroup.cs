using System.Collections.Concurrent;

namespace EasyNetworking.AspNetCore.Servers.MjpegStreamer.Models
{
    internal sealed class MjpegGroup
    {
        public ConcurrentDictionary<string, Client> Clients { get; } = new();
    }
}