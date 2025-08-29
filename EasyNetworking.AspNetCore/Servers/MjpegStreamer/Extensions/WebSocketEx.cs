using EasyNetworking.AspNetCore.Servers.MjpegStreamer.Models;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;


namespace EasyNetworking.AspNetCore.Servers.MjpegStreamer.Extensions
{
    public static class WebSocketEx
    {
        public static async Task MjpegForwardAsync(this WebSocket webSocket, HttpContext httpContext)
        {
            string? groupName = httpContext.Request.Query["groupName"];
            if (string.IsNullOrEmpty(httpContext.Connection.Id) || string.IsNullOrEmpty(groupName))
                return;

            if (!Cache.Streamers.ContainsKey(groupName))
                Cache.Streamers.TryAdd(groupName, httpContext.Connection.Id);
            else
                return;  //this group already streaming

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
                        if (webSocket.State == WebSocketState.Open && Cache.Sessions.TryGetValue(groupName, out List<Client>? clients))
                        {
                            while (clients.Any(a => a.Context!.RequestAborted.IsCancellationRequested == true))
                                clients.Remove(clients.First(a => a.Context!.RequestAborted.IsCancellationRequested == true));
                            if (clients.Count == 0)
                                Cache.Sessions.TryRemove(groupName, out _);

                            foreach (Client client in clients)
                            {
                                if (!client.Response!.HasStarted)
                                {
                                    string boundary = "--boundary";
                                    client.MjpegWriter = new MjpegWriter(client.Response.Body, boundary);
                                    client.Response.StatusCode = 200;
                                    client.Response.Headers.ContentType = $"multipart/x-mixed-replace; boundary={boundary}";
                                }

                                await client.MjpegWriter!.WriteAsync(buffer[0..offset]);
                            }
                        }

                        buffer = new byte[ushort.MaxValue];
                        offset = 0;
                    }
                }
                catch
                {
                    buffer = new byte[ushort.MaxValue];
                    offset = 0;
                }
            } while (webSocket.State == WebSocketState.Open);

            Cache.Streamers.TryRemove(groupName, out _);

            if (Cache.Sessions.TryGetValue(groupName, out List<Client>? disconnectClients))
                foreach (Client client in disconnectClients)
                    client.StreamingCts!.Cancel();
            Cache.Sessions.TryRemove(groupName, out _);
        }
    }
}