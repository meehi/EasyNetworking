using EasyNetworking.AspNetCore.Servers.WebSocketProxy.Models;
using EasyNetworking.AspNetCore.Servers.WebSocketProxy.Services;
using Microsoft.AspNetCore.Http;
using System.Buffers;
using System.Net.WebSockets;

namespace EasyNetworking.AspNetCore.Servers.WebSocketProxy.Extensions
{
    internal static class WebSocketEx
    {
        private const int ReceiveBufferSize = 16 * 1024;
        private const int MaxMessageSize = 1024 * 1024;

        public static async Task ProxyForwardAsync(this WebSocket webSocket, HttpContext httpContext, WebSocketProxyService proxyService)
        {
            string? groupName = httpContext.Request.Query["groupName"];
            string connectionId = httpContext.Connection.Id;

            if (string.IsNullOrEmpty(connectionId) || string.IsNullOrEmpty(groupName))
                return;

            WebSocketSession session = proxyService.RegisterSession(groupName, connectionId, webSocket);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(httpContext.RequestAborted);
            CancellationToken cancellationToken = cts.Token;

            Task receiveTask = ReceiveMessagesAsync(
                webSocket,
                groupName,
                connectionId,
                proxyService,
                cancellationToken);

            Task sendTask = ProcessOutgoingMessagesAsync(
                session,
                cancellationToken);

            try
            {
                // Wait until either direction terminates.
                Task completedTask = await Task.WhenAny(receiveTask, sendTask);
                await completedTask;
            }
            catch (OperationCanceledException)
            {
                // The connection has been cancelled.
            }
            catch (WebSocketException)
            {
                // The connection has been interrupted.
            }
            finally
            {
                // Remove session and complete its outgoing channel.
                proxyService.RemoveSession(groupName, session);

                // Stop the remaining operation.
                cts.Cancel();

                // Wait for both operations to finish.
                await ObserveTaskAsync(receiveTask);
                await ObserveTaskAsync(sendTask);

                // Complete the WebSocket closing handshake.
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
                        // The connection is already closed.
                    }
                }
            }
        }

        private static async Task ReceiveMessagesAsync(WebSocket webSocket, string groupName, string connectionId, WebSocketProxyService proxyService, CancellationToken cancellationToken)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(ReceiveBufferSize);

            try
            {
                var messageBuffer = new ArrayBufferWriter<byte>(ReceiveBufferSize);

                while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    ValueWebSocketReceiveResult result = await webSocket.ReceiveAsync(buffer.AsMemory(), cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    if (messageBuffer.WrittenCount + result.Count > MaxMessageSize)
                        throw new WebSocketException(WebSocketError.HeaderError, "Maximum WebSocket message size exceeded.");

                    messageBuffer.Write(buffer.AsSpan(0, result.Count));
                    if (!result.EndOfMessage)
                        continue;

                    var message = new WebSocketMessage(messageBuffer.WrittenMemory.ToArray(), result.MessageType);

                    await proxyService.BroadcastAsync(
                        groupName,
                        connectionId,
                        message,
                        cancellationToken);

                    messageBuffer.Clear();
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static async Task ProcessOutgoingMessagesAsync(WebSocketSession session, CancellationToken cancellationToken)
        {
            await foreach (WebSocketMessage message in session.OutgoingChannel.Reader.ReadAllAsync(cancellationToken))
            {
                if (session.WebSocket.State != WebSocketState.Open)
                    break;

                await session.WebSocket.SendAsync(
                    message.Data.AsMemory(),
                    message.MessageType,
                    true,
                    cancellationToken);
            }
        }

        private static async Task ObserveTaskAsync(Task task)
        {
            try
            {
                await task;
            }
            catch
            {
                // The connection is already terminating.
            }
        }
    }
}