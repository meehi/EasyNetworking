using EasyNetworking.NetCore.Clients.WebSocketProxy.Models;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace EasyNetworking.NetCore.Clients.WebSocketProxy
{
    public class WsProxyClient(Uri host)
    {
        #region Private variables
        private readonly Uri _host = host;
        private ClientWebSocket? _clientWebSocket;
        private bool _disconnectRequested;
        private TimeSpan? _reconnectInterval;

        private readonly ConcurrentDictionary<Type, Delegate> _handlers = new();
        private readonly ConcurrentDictionary<Guid, Delegate> _replyHandlers = new();
        private readonly ConcurrentDictionary<object, Guid> _replyToObjects = new();
        #endregion

        #region Public methods
        public async Task<bool> ConnectAsync(TimeSpan? withReconnect = null)
        {
            if (withReconnect.HasValue)
                _reconnectInterval = withReconnect;
            _disconnectRequested = false;
            return await ConnectWithReconnectAsync();
        }

        public async Task DisconnectAsync()
        {
            _disconnectRequested = true;
            if (_clientWebSocket == null || _clientWebSocket.State != WebSocketState.Open)
                return;

            try
            {
                await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
            catch
            {
            }
            finally
            {
                _clientWebSocket.Dispose();
            }
        }

        public bool IsConnected() => _clientWebSocket?.State == WebSocketState.Open;

        public void On<T>(Action<T> callback) => _handlers.TryAdd(typeof(T), callback);

        public async Task SendAsync<T>(T obj)
        {
            if (!IsConnected())
                return;

            if (obj is byte[] bytes)
            {
                await _clientWebSocket!.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, true, CancellationToken.None);
                return;
            }

            MessageWrapper wrapper = new() { Id = Guid.NewGuid(), MessageType = typeof(T).FullName, Message = obj };
            byte[] payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(wrapper));
            await _clientWebSocket!.SendAsync(new ArraySegment<byte>(payload, 0, payload.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async Task<TResult?> SendAsync<T, TResult>(T obj, CancellationTokenSource? cts = null)
        {
            if (!IsConnected())
                return default;

            MessageWrapper wrapper = new() { Id = Guid.NewGuid(), MessageType = typeof(T).FullName, Message = obj };
            cts ??= new();
            var tcs = new TaskCompletionSource<TResult?>();

            _replyHandlers.TryAdd(wrapper.Id.Value, new Action<object?>(param =>
            {
                var result = Deserialize(param, typeof(TResult));
                tcs.TrySetResult((TResult?)result);
            }));

            byte[] payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(wrapper));
            await _clientWebSocket!.SendAsync(new ArraySegment<byte>(payload, 0, payload.Length), WebSocketMessageType.Text, true, CancellationToken.None);

            try
            {
                return await tcs.Task.WaitAsync(cts.Token);
            }
            catch
            {
                return default;
            }
            finally
            {
                _replyHandlers.TryRemove(wrapper.Id.Value, out _);
            }
        }

        public async Task ReplyToAsync<T>(object replyToObject, T answer)
        {
            if (!IsConnected() || !_replyToObjects.TryRemove(replyToObject, out Guid replyId))
                return;

            MessageWrapper wrapper = new() { MessageType = typeof(T).FullName, Message = answer, ReplyId = replyId };
            byte[] payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(wrapper));
            await _clientWebSocket!.SendAsync(new ArraySegment<byte>(payload, 0, payload.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        #endregion

        #region Private methods
        private async Task<bool> ConnectWithReconnectAsync()
        {
            _clientWebSocket?.Dispose();
            _clientWebSocket = new();
            try
            {
                await _clientWebSocket.ConnectAsync(_host, CancellationToken.None);
                StartReceiveMessages();
                return true;
            }
            catch (Exception ex)
            {
                HandleReconnect();
                return false;
            }
        }

        private void HandleReconnect()
        {
            if (!_disconnectRequested && _reconnectInterval.HasValue)
                _ = Task.Delay(_reconnectInterval.Value).ContinueWith(_ => ConnectAsync());
        }

        private void StartReceiveMessages()
        {
            Task.Run(async () =>
            {
                var buffer = new byte[ushort.MaxValue];
                var offset = 0;
                try
                {
                    while (IsConnected())
                    {
                        var result = await _clientWebSocket!.ReceiveAsync(new ArraySegment<byte>(buffer, offset, buffer.Length - offset), CancellationToken.None);
                        offset += result.Count;

                        if (!result.EndOfMessage)
                        {
                            Array.Resize(ref buffer, buffer.Length + ushort.MaxValue);
                            continue;
                        }

                        ProcessMessage(buffer, offset, result.MessageType);

                        buffer = new byte[ushort.MaxValue];
                        offset = 0;
                    }
                }
                catch
                {
                }

                HandleReconnect();
            });
        }


        private void ProcessMessage(byte[] buffer, int length, WebSocketMessageType type)
        {
            if (type == WebSocketMessageType.Binary)
            {
                if (_handlers.TryGetValue(typeof(byte[]), out var handler))
                    handler.DynamicInvoke([buffer[0..length]]);
                return;
            }

            var response = JsonSerializer.Deserialize<MessageWrapper>(buffer[0..length]);
            if (response == null)
                return;

            if (response.ReplyId.HasValue && _replyHandlers.TryRemove(response.ReplyId.Value, out var replyCb))
            {
                var targetType = replyCb.Method.GetParameters().FirstOrDefault()?.ParameterType;
                if (targetType != null)
                    replyCb.DynamicInvoke([Deserialize(response.Message, targetType)]);
                return;
            }

            var handlerEntry = _handlers.FirstOrDefault(h => h.Key.FullName == response.MessageType);
            if (handlerEntry.Value != null)
            {
                var finalMsg = Deserialize(response.Message, handlerEntry.Key);
                if (finalMsg != null)
                {
                    if (response.Id.HasValue)
                        _replyToObjects.TryAdd(finalMsg, response.Id.Value);
                    handlerEntry.Value.DynamicInvoke([finalMsg]);
                }
            }
        }

        private static object? Deserialize(object? message, Type type)
        {
            if (message == null)
                return null;
            if (message is JsonElement element)
                return element.Deserialize(type);
            if (type.IsInstanceOfType(message))
                return message;
            return JsonSerializer.Deserialize(message.ToString()!, type);
        }
        #endregion
    }
}