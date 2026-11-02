using EasyNetworking.AspNetCore.Servers.WebSocketProxy.Models;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading.Channels;

namespace EasyNetworking.AspNetCore.Servers.WebSocketProxy.Services
{
    internal sealed class WebSocketProxyService
    {
        private readonly ConcurrentDictionary<string, WebSocketGroup> _groups = new();

        private readonly Lock _syncRoot = new();

        /// <summary>
        /// Registers a new WebSocket session in the specified group.
        /// </summary>
        public WebSocketSession RegisterSession(string groupName, string connectionId, WebSocket webSocket)
        {
            lock (_syncRoot)
            {
                WebSocketGroup group = _groups.GetOrAdd(groupName, _ => new WebSocketGroup());

                var session = new WebSocketSession
                {
                    Id = connectionId,
                    WebSocket = webSocket
                };

                if (!group.Sessions.TryAdd(connectionId, session))
                    throw new InvalidOperationException($"Session '{connectionId}' is already registered.");

                return session;
            }
        }

        /// <summary>
        /// Removes a WebSocket session from its group.
        /// Removes the group when it becomes empty.
        /// </summary>
        public void RemoveSession(string groupName, WebSocketSession session)
        {
            lock (_syncRoot)
            {
                if (!_groups.TryGetValue(groupName, out WebSocketGroup? group))
                    return;

                if (group.Sessions.TryGetValue(session.Id, out WebSocketSession? current) &&
                    ReferenceEquals(current, session))
                {
                    group.Sessions.TryRemove(session.Id, out _);

                    session.OutgoingChannel.Writer.TryComplete();
                }

                if (group.Sessions.IsEmpty)
                {
                    ((ICollection<KeyValuePair<string, WebSocketGroup>>)_groups)
                        .Remove(new(groupName, group));
                }
            }
        }

        /// <summary>
        /// Broadcasts a message to every session except the sender.
        /// </summary>
        public async Task BroadcastAsync(string groupName, string senderId, WebSocketMessage message, CancellationToken cancellationToken = default)
        {
            if (!_groups.TryGetValue(groupName, out WebSocketGroup? group))
                return;

            WebSocketSession[] recipients = group.Sessions.Values
                .Where(session => session.Id != senderId)
                .ToArray();

            if (recipients.Length == 0)
                return;

            var tasks = new Task[recipients.Length];

            for (int i = 0; i < recipients.Length; i++)
            {
                tasks[i] = EnqueueMessageAsync(
                    recipients[i],
                    message,
                    cancellationToken);
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Enqueues a message into the recipient's outgoing channel.
        /// </summary>
        private static async Task EnqueueMessageAsync(WebSocketSession session, WebSocketMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await session.OutgoingChannel.Writer.WriteAsync(
                    message,
                    cancellationToken);
            }
            catch (ChannelClosedException)
            {
                // The recipient session has already been closed.
            }
        }
    }
}