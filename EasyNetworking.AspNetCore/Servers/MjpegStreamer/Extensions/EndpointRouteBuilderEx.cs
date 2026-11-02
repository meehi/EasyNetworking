using EasyNetworking.AspNetCore.Servers.MjpegStreamer.Models;
using EasyNetworking.AspNetCore.Servers.MjpegStreamer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Net.WebSockets;

namespace EasyNetworking.AspNetCore.Servers.MjpegStreamer.Extensions
{
    public static class EndpointRouteBuilderEx
    {
        public static void UseMjpegStreamer(this IEndpointRouteBuilder app, string streamEndPoint, string liveStreamEndPoint)
        {
            var streamerService = new MjpegStreamerService();

            // WebSocket endpoint - receives camera frames.
            app.MapGet(streamEndPoint, async (context) =>
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                await webSocket.MjpegForwardAsync(context, streamerService);
            });

            // HTTP endpoint - broadcasts MJPEG frames to clients.
            app.MapGet(liveStreamEndPoint, async (context) =>
            {
                string? groupName = context.Request.Query["groupName"];
                if (string.IsNullOrEmpty(groupName))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                var client = new Client(context.Connection.Id, context.Response.Body, context.RequestAborted);
                if (!streamerService.RegisterClient(groupName, client))
                {
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    return;
                }

                context.Response.ContentType ="multipart/x-mixed-replace; boundary=boundary";
                context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
                context.Response.Headers.Pragma = "no-cache";
                context.Response.Headers.Expires = "0";

                var writer = new MjpegWriter(context.Response.Body);

                try
                {
                    await foreach (byte[] frame in client.FrameChannel.Reader.ReadAllAsync(context.RequestAborted))
                    {
                        await writer.WriteFrameAsync(frame, context.RequestAborted);
                    }
                }
                catch (OperationCanceledException)
                {
                    // The client disconnected.
                }
                finally
                {
                    streamerService.RemoveClient(groupName, client);
                }
            });
        }
    }
}