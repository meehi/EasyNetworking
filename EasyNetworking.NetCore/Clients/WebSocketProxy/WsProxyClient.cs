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
        private readonly ConcurrentDictionary<Type, Delegate> _handlers = new();
        private readonly ConcurrentDictionary<Guid, Delegate> _replyHandlers = new();
        private readonly ConcurrentDictionary<object, Guid> _replyToObjects = new();
        #endregion

        #region Public methods
        public async Task<bool> TryConnectAsync()
        {
            _disconnectRequested = false;
            _clientWebSocket = new();
            bool connected = false;
            try
            {
                await _clientWebSocket.ConnectAsync(_host, CancellationToken.None);
                connected = true;
            }
            catch
            {
            }
            if (connected)
                StartReceiveMessages();

            return connected;
        }

        public async Task TryDisconnectAsync()
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
        }

        public bool IsConnected()
        {
            return _clientWebSocket?.State == WebSocketState.Open;
        }

        public void On<T>(Action<T> callback)
        {
            if (!_handlers.ContainsKey(typeof(T)))
                _handlers.TryAdd(typeof(T), callback);
        }

        public async Task SendAsync<T>(T obj)
        {
            if (_clientWebSocket == null || _clientWebSocket.State != WebSocketState.Open)
                return;

            if (typeof(T) != typeof(byte[]))
            {
                MessageWrapper<T> message = new() { MessageType = typeof(T).FullName, Message = obj };
                await _clientWebSocket.SendAsync(new(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message))), WebSocketMessageType.Text, true, new CancellationTokenSource().Token);
            }
            else
            {
                byte[] bytes = (byte[])Convert.ChangeType(obj, typeof(byte[]))!;
                await _clientWebSocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Binary, true, new CancellationTokenSource().Token);
            }
        }

        public async Task<TResult?> SendAsync<T, TResult>(T obj, CancellationTokenSource? cts = null)
        {
            if (_clientWebSocket == null || _clientWebSocket.State != WebSocketState.Open)
                return default;

            MessageWrapper<T> message = new() { Id = Guid.NewGuid(), MessageType = typeof(T).FullName, Message = obj };

            if (cts == null)
                cts = new();
            TaskCompletionSource<TResult?> tcs = new();
            _replyHandlers.TryAdd(message.Id.Value, new Action<object?>(
                (param) =>
                {
                    if (param == null)
                        tcs.SetResult(default);
                    else
                    {
                        object? obj = Deserialize(param.ToString()!, typeof(TResult));
                        tcs.SetResult((TResult)Convert.ChangeType(obj, typeof(TResult))!);
                    }
                }));

            await _clientWebSocket.SendAsync(new(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message))), WebSocketMessageType.Text, true, new CancellationTokenSource().Token);

            TResult? result = default;
            try
            {
                result = await tcs.Task.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
            }

            _replyHandlers.TryRemove(message.Id.Value, out Delegate? replyCallback);

            return result;
        }

        public async Task ReplyToAsync<T>(object replyToObject, T answer)
        {
            if (_clientWebSocket == null || _clientWebSocket.State != WebSocketState.Open)
                return;
            if (!_replyToObjects.Any(a => a.Key == replyToObject))
                return;

            _replyToObjects.TryRemove(replyToObject, out Guid replyID);

            MessageWrapper<T> message = new() { MessageType = typeof(T).FullName, Message = answer, ReplyId = replyID };

            await _clientWebSocket.SendAsync(new(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message))), WebSocketMessageType.Text, true, new CancellationTokenSource().Token);
        }
        #endregion

        #region Private methods
        private void StartReceiveMessages()
        {
            if (_clientWebSocket == null)
                return;

            new Task(async () =>
            {
                CancellationTokenSource cts = new();
                var buffer = new byte[ushort.MaxValue];
                var offset = 0;
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        WebSocketReceiveResult result = await _clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer, offset, ushort.MaxValue), cts.Token);

                        offset += result.Count;
                        if (!result.EndOfMessage)
                            Array.Resize(ref buffer, buffer.Length + ushort.MaxValue);
                        else
                        {
                            if (result.MessageType == WebSocketMessageType.Binary)
                            {
                                if (_handlers.Any(a => a.Key.FullName == typeof(byte[]).FullName))
                                {
                                    KeyValuePair<Type, Delegate> handler = _handlers.First(a => a.Key.FullName == typeof(byte[]).FullName);
                                    handler.Value.DynamicInvoke([buffer[0..offset]]);
                                }
                            }
                            else
                            {
                                MessageWrapper? responseMessage = JsonSerializer.Deserialize<MessageWrapper>(Encoding.UTF8.GetString(buffer[0..offset]));
                                if (responseMessage != null)
                                {
                                    if (responseMessage.ReplyId != null && _replyHandlers.Any(a => a.Key == responseMessage.ReplyId.Value))
                                    {
                                        _replyHandlers.TryRemove(responseMessage.ReplyId.Value, out Delegate? replyCallback);
                                        if (responseMessage.Message != null)
                                            replyCallback?.DynamicInvoke([responseMessage.Message]);
                                    }

                                    if (_handlers.Any(a => a.Key.FullName == responseMessage.MessageType))
                                    {
                                        KeyValuePair<Type, Delegate> handler = _handlers.First(a => a.Key.FullName == responseMessage.MessageType);

                                        object? receivedMessage = responseMessage.Message;
                                        if (receivedMessage != null)
                                            receivedMessage = Deserialize(receivedMessage.ToString()!, handler.Key);

                                        if (receivedMessage != null && responseMessage.Id != null)
                                            _replyToObjects.TryAdd(receivedMessage, responseMessage.Id.Value);

                                        handler.Value.DynamicInvoke([receivedMessage]);
                                    }
                                }
                            }

                            buffer = new byte[ushort.MaxValue];
                            offset = 0;
                        }
                    }
                    catch
                    {
                        cts.Cancel();
                    }
                }

                if (!_disconnectRequested)
                    await TryConnectAsync();
            }).Start();
        }

        private static object? Deserialize(string message, Type type)
        {
            if (type == typeof(bool))
                return Convert.ToBoolean(message);
            if (type == typeof(byte))
                return Convert.ToByte(message);
            if (type == typeof(char))
                return Convert.ToChar(message);
            if (type == typeof(DateTime))
                return Convert.ToDateTime(message);
            if (type == typeof(decimal))
                return Convert.ToDecimal(message);
            if (type == typeof(double))
                return Convert.ToDouble(message);
            if (type == typeof(float))
                return Convert.ToSingle(message);
            if (type == typeof(int) || type == typeof(int))
                return Convert.ToInt32(message);
            if (type == typeof(short))
                return Convert.ToInt16(message);
            if (type == typeof(long))
                return Convert.ToInt64(message);
            if (type == typeof(uint) || type == typeof(uint))
                return Convert.ToUInt32(message);
            if (type == typeof(ushort))
                return Convert.ToUInt16(message);
            if (type == typeof(ulong))
                return Convert.ToUInt64(message);
            if (type == typeof(string))
                return Convert.ToString(message);

            return JsonSerializer.Deserialize(message, type);
        }
        #endregion
    }
}