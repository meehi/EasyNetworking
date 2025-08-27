using EasyNetworking.AspNetCore.Servers.WebSocketProxy.Models;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;

namespace EasyNetworking.AspNetCore.Servers.WebSocketProxy.Extensions
{
    public static class WebSocketEx
    {
        public static async Task ProxyForwardAsync(this WebSocket webSocket, HttpContext httpContext)
        {
            string? groupName = httpContext.Request.Query["groupName"];
            if (string.IsNullOrEmpty(httpContext.Connection.Id) || string.IsNullOrEmpty(groupName))
                return;

            if (!Cache.Sessions.TryGetValue(groupName, out List<WebSocketSession>? sessions))
                Cache.Sessions.TryAdd(groupName, [new() { Id = httpContext.Connection.Id, WebSocket = webSocket }]);
            else
                if (!sessions.Any(a => a.Id == httpContext.Connection.Id))
                sessions.Add(new() { Id = httpContext.Connection.Id, WebSocket = webSocket });

            var buffer = new byte[ushort.MaxValue];
            var offset = 0;
            do
            {
                try
                {
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer, offset, ushort.MaxValue), CancellationToken.None);

                    offset += result.Count;
                    if (!result.EndOfMessage)
                        Array.Resize(ref buffer, buffer.Length + ushort.MaxValue);
                    else
                    {
                        if (webSocket.State == WebSocketState.Open && Cache.Sessions[groupName].Any(a => a.Id != httpContext.Connection.Id))
                            foreach (WebSocketSession session in Cache.Sessions[groupName].Where(a => a.Id != httpContext.Connection.Id))
                                await session.WebSocket!.SendAsync(new ArraySegment<byte>(buffer[0..offset]), result.MessageType, true, CancellationToken.None);

                        buffer = new byte[ushort.MaxValue];
                        offset = 0;
                    }
                }
                catch
                {
                }
            } while (webSocket.State == WebSocketState.Open);

            WebSocketSession currentSession = Cache.Sessions[groupName].First(a => a.Id == httpContext.Connection.Id);
            Cache.Sessions[groupName].Remove(currentSession);
            if (Cache.Sessions[groupName].Count == 0)
                Cache.Sessions.TryRemove(groupName, out List<WebSocketSession>? removedSessions);
        }
    }
}