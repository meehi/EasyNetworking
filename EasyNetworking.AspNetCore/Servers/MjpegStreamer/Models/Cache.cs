using System.Collections.Concurrent;

namespace EasyNetworking.AspNetCore.Servers.MjpegStreamer.Models
{
    internal static class Cache
    {
        public static readonly ConcurrentDictionary<string, string> Streamers = new();
        public static readonly ConcurrentDictionary<string, ConcurrentBag<Client>> Sessions = new();
    }
}