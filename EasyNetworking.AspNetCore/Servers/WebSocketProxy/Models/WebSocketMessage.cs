using System.Net.WebSockets;

namespace EasyNetworking.AspNetCore.Servers.WebSocketProxy.Models
{
    internal sealed record WebSocketMessage(byte[] Data, WebSocketMessageType MessageType);
}