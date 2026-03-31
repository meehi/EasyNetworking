using EasyNetworking.AspNetCore.Servers.MjpegStreamer.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace EasyNetworking.AspNetCore.Servers.MjpegStreamer.Extensions
{
    public static class EndpointRouteBuilderEx
    {
        public static void UseMjpegStreamer(this IEndpointRouteBuilder app, string streamEndPoint, string liveStreamEndPoint)
        {
            app.MapGet(streamEndPoint, async (HttpContext context) =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    await webSocket.MjpegForwardAsync(context);
                }
                else
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
            });

            app.MapGet(liveStreamEndPoint, async (HttpContext context) =>
            {
                string? groupName = context.Request.Query["groupName"];
                if (string.IsNullOrEmpty(groupName) || !Cache.Streamers.ContainsKey(groupName))
                    return;

                Client client = new(context.Connection.Id, context);

                var sessions = Cache.Sessions.GetOrAdd(groupName, _ => new ConcurrentBag<Client>());
                sessions.Add(client);

                context.Response.ContentType = "multipart/x-mixed-replace; boundary=--boundary";
                context.Response.StatusCode = 200;

                MjpegWriter writer = new(context.Response.Body);

                try
                {
                    await foreach (var frame in client.FrameChannel.Reader.ReadAllAsync(context.RequestAborted))
                    {
                        await writer.WriteFrameAsync(frame, context.RequestAborted);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    if (Cache.Sessions.TryGetValue(groupName, out var clients))
                    {
                        Cache.Sessions[groupName] = new ConcurrentBag<Client>(clients.Where(c => c.Id != client.Id));
                    }
                }
            });
        }
    }
}