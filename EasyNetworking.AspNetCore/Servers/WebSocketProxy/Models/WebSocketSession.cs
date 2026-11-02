using System.Net.WebSockets;
using System.Threading.Channels;

namespace EasyNetworking.AspNetCore.Servers.WebSocketProxy.Models
{
    internal sealed class WebSocketSession
    {
        public required string Id { get; init; }

        public required WebSocket WebSocket { get; init; }

        public Channel<WebSocketMessage> OutgoingChannel { get; } =
            Channel.CreateBounded<WebSocketMessage>(
                new BoundedChannelOptions(32)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.Wait
                });
    }
}