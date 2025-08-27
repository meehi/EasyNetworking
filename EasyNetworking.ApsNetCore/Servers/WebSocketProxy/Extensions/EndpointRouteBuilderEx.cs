using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Net.WebSockets;

namespace EasyNetworking.ApsNetCore.Servers.WebSocketProxy.Extensions
{
    public static class EndpointRouteBuilderEx
    {
        public static void UseWebSocketProxy(this IEndpointRouteBuilder app, string endPoint)
        {
            app.MapGet(endPoint,
                async (HttpRequest request, HttpResponse response) =>
                {
                    if (request.HttpContext.WebSockets.IsWebSocketRequest)
                    {
                        using WebSocket webSocket = await request.HttpContext.WebSockets.AcceptWebSocketAsync();
                        await webSocket.ProxyForwardAsync(request.HttpContext);
                    }
                    else
                        request.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                });
        }
    }
}