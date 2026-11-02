using EasyNetworking.AspNetCore.Servers.MjpegStreamer.Services;
using Microsoft.AspNetCore.Http;
using System.Buffers;
using System.Net.WebSockets;

namespace EasyNetworking.AspNetCore.Servers.MjpegStreamer.Extensions
{
    internal static class WebSocketEx
    {
        private const int ReceiveBufferSize = 16 * 1024;
        private const int MaxFrameSize = 4 * 1024 * 1024;

        internal static async Task MjpegForwardAsync(this WebSocket webSocket,HttpContext httpContext, MjpegStreamerService streamerService)
        {
            string? groupName = httpContext.Request.Query["groupName"];
            string connectionId = httpContext.Connection.Id;

            if (string.IsNullOrEmpty(groupName))
            {
                await RejectConnectionAsync(webSocket);
                return;
            }

            if (!streamerService.RegisterStreamer(groupName, connectionId))
            {
                await RejectConnectionAsync(webSocket);
                return;
            }

            CancellationToken cancellationToken = httpContext.RequestAborted;

            byte[] buffer = ArrayPool<byte>.Shared.Rent(ReceiveBufferSize);
            var frameBuffer = new ArrayBufferWriter<byte>(ReceiveBufferSize);

            try
            {
                while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    ValueWebSocketReceiveResult result = await webSocket.ReceiveAsync(buffer.AsMemory(), cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    if (result.MessageType != WebSocketMessageType.Binary)
                    {
                        await CloseConnectionAsync(webSocket, WebSocketCloseStatus.InvalidMessageType, "Only binary messages are supported.");
                        break;
                    }

                    if (frameBuffer.WrittenCount + result.Count > MaxFrameSize)
                    {
                        await CloseConnectionAsync(webSocket, WebSocketCloseStatus.MessageTooBig, "Maximum frame size exceeded.");
                        break;
                    }

                    frameBuffer.Write(buffer.AsSpan(0, result.Count));

                    if (!result.EndOfMessage)
                        continue;

                    if (frameBuffer.WrittenCount > 0)
                    {
                        byte[] frame = frameBuffer.WrittenMemory.ToArray();
                        streamerService.BroadcastFrame(groupName, frame);
                    }

                    frameBuffer.Clear();
                }
            }
            catch (OperationCanceledException)
            {
                // The HTTP connection was aborted.
            }
            catch (WebSocketException)
            {
                // The WebSocket connection was interrupted.
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                streamerService.RemoveStreamer(groupName, connectionId);

                if (webSocket.State == WebSocketState.CloseReceived)
                {
                    try
                    {
                        await webSocket.CloseOutputAsync(
                            webSocket.CloseStatus ??
                                WebSocketCloseStatus.NormalClosure,
                            webSocket.CloseStatusDescription,
                            CancellationToken.None);
                    }
                    catch (WebSocketException)
                    {
                        // The connection was already closed.
                    }
                }
            }
        }

        private static async Task RejectConnectionAsync(WebSocket webSocket)
        {
            await CloseConnectionAsync(webSocket, WebSocketCloseStatus.PolicyViolation, "Invalid or already active stream.");
        }

        private static async Task CloseConnectionAsync(WebSocket webSocket, WebSocketCloseStatus status, string description)
        {
            if (webSocket.State != WebSocketState.Open)
                return;

            try
            {
                await webSocket.CloseOutputAsync(status, description, CancellationToken.None);
            }
            catch (WebSocketException)
            {
                // The connection was already closed.
            }
        }
    }
}