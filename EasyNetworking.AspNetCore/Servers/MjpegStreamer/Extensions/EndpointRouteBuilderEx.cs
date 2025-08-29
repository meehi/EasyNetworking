using EasyNetworking.AspNetCore.Servers.MjpegStreamer.Models;
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
            app.MapGet(streamEndPoint,
                async (HttpRequest request, HttpResponse response) =>
                {
                    if (request.HttpContext.WebSockets.IsWebSocketRequest)
                    {
                        using WebSocket webSocket = await request.HttpContext.WebSockets.AcceptWebSocketAsync();
                        await webSocket.MjpegForwardAsync(request.HttpContext);
                    }
                    else
                        request.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                });

            app.MapGet(liveStreamEndPoint,
                (HttpRequest request, HttpResponse response) =>
                {
                    string? groupName = request.Query["groupName"];
                    if (string.IsNullOrEmpty(request.HttpContext.Connection.Id) || string.IsNullOrEmpty(groupName))
                        return;

                    if (!Cache.Streamers.ContainsKey(groupName))  //not streaming
                        return;

                    CancellationTokenSource cts = new();
                    Client currentClient = new()
                    {
                        Id = request.HttpContext.Connection.Id,
                        Context = request.HttpContext,
                        Response = request.HttpContext.Response,
                        StreamingCts = new()
                    };

                    if (!Cache.Sessions.TryGetValue(groupName, out List<Client>? clients))
                        Cache.Sessions.TryAdd(groupName, [currentClient]);
                    else
                        if (!clients.Any(a => a.Id == request.HttpContext.Connection.Id))
                        clients.Add(currentClient);

                    currentClient.StreamingCts.Token.WaitHandle.WaitOne();
                });
        }
    }
}