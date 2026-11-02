using EasyNetworking.AspNetCore.Servers.WebSocketProxy.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Net.WebSockets;

namespace EasyNetworking.AspNetCore.Servers.WebSocketProxy.Extensions
{
    public static class EndpointRouteBuilderEx
    {
        public static void UseWebSocketProxy(this IEndpointRouteBuilder app, string endPoint)
        {
            // Create one proxy service instance for this application endpoint.
            var proxyService = new WebSocketProxyService();

            app.MapGet(endPoint,
                async (httpContext) =>
                {
                    if (httpContext.WebSockets.IsWebSocketRequest)
                    {
                        using WebSocket webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
                        await webSocket.ProxyForwardAsync(httpContext, proxyService);
                    }
                    else
                        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                });
        }
    }
}