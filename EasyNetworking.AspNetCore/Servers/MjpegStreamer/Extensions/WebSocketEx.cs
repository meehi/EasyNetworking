using EasyNetworking.AspNetCore.Servers.MjpegStreamer.Models;
using Microsoft.AspNetCore.Http;
using System.Buffers;
using System.Net.WebSockets;


namespace EasyNetworking.AspNetCore.Servers.MjpegStreamer.Extensions
{
    public static class WebSocketEx
    {
        public static async Task MjpegForwardAsync(this WebSocket webSocket, HttpContext httpContext)
        {
            string? groupName = httpContext.Request.Query["groupName"];
            if (string.IsNullOrEmpty(groupName) || !Cache.Streamers.TryAdd(groupName, httpContext.Connection.Id))
                return;

            var pool = ArrayPool<byte>.Shared;
            byte[] buffer = pool.Rent(ushort.MaxValue);
            int offset = 0;

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer, offset, buffer.Length - offset), CancellationToken.None);
                    offset += result.Count;

                    if (!result.EndOfMessage)
                    {
                        var newBuffer = pool.Rent(buffer.Length + ushort.MaxValue);
                        Buffer.BlockCopy(buffer, 0, newBuffer, 0, offset);
                        pool.Return(buffer);
                        buffer = newBuffer;
                        continue;
                    }

                    if (Cache.Sessions.TryGetValue(groupName, out var clients))
                    {
                        byte[] frameToSend = new byte[offset];
                        Buffer.BlockCopy(buffer, 0, frameToSend, 0, offset);

                        foreach (var client in clients)
                            client.FrameChannel.Writer.TryWrite(frameToSend);
                    }

                    offset = 0;
                }
            }
            finally
            {
                pool.Return(buffer);
                Cache.Streamers.TryRemove(groupName, out _);
                if (Cache.Sessions.TryRemove(groupName, out var clients))
                    foreach (var client in clients)
                        client.FrameChannel.Writer.Complete();
            }
        }
    }
}