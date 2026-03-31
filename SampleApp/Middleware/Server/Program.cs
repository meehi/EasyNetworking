using EasyNetworking.AspNetCore.Servers.MjpegStreamer.Extensions;
using EasyNetworking.AspNetCore.Servers.WebSocketProxy.Extensions;
using SharedData.Models;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(120) });

app.UseWebSocketProxy("/ws");

app.UseMjpegStreamer(
    streamEndPoint: $"/{EndPoints.STREAM}",
    liveStreamEndPoint: $"/{EndPoints.LIVE}"
);

app.MapGet("/", () => "Hello World!");

app.Run();