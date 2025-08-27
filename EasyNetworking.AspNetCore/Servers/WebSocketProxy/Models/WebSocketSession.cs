using System.Net.WebSockets;

namespace EasyNetworking.AspNetCore.Servers.WebSocketProxy.Models
{
    internal class WebSocketSession
    {
        public string? Id { get; set; }
        public WebSocket? WebSocket { get; set; }
    }
}