using EasyNetworking.AspNetCore.Servers.WebSocketProxy.Extensions;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(120) });

app.UseWebSocketProxy("/ws");

app.MapGet("/", () => "Hello World!");

app.Run();
