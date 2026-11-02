using System.Collections.Concurrent;

namespace EasyNetworking.AspNetCore.Servers.WebSocketProxy.Models
{
    internal sealed class WebSocketGroup
    {
        public ConcurrentDictionary<string, WebSocketSession> Sessions { get; } = new();
    }
}