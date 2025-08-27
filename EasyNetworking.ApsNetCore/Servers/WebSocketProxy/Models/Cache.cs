using System.Collections.Concurrent;

namespace EasyNetworking.ApsNetCore.Servers.WebSocketProxy.Models
{
    internal static class Cache
    {
        public static ConcurrentDictionary<string, List<WebSocketSession>> Sessions = new();
    }
}