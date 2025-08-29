using System.Collections.Concurrent;

namespace EasyNetworking.AspNetCore.Servers.MjpegStreamer.Models
{
    internal static class Cache
    {
        public static ConcurrentDictionary<string, string> Streamers = new();
        public static ConcurrentDictionary<string, List<Client>> Sessions = new();
    }
}