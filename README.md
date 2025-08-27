# WebSocketProxy
Type based websocket communication between clients through a Middleware server (proxy-forwarder).

The EasyNetworking library allows you to create client to client websocket communication (like chat or gaming applications) through a Middleware server. It's similiar to a hub client-server communication but without the limitation of message sizes. With EasyWebSocketProxy you can achive sending large binary data as well.

## Usage:
```C#
//send message [CLIENT 1]
Uri uri = new($"wss://localhost:7298/ws?groupName=group_1");  //always include a group name in the URL
WsProxyClient wsProxyClient = new(uri);
await wsProxyClient.TryConnectAsync();
//send some typed message to the other client with the same group connected
await wsProxyClient.SendAsync<SocketMessage>(new() { Message = "Hello from Client 1" });
//send binary data
byte[] data = Encoding.UTF8.GetBytes("Hello from Client 1! This is going to be a byte array message!");
await wsProxyClient.SendAsync(data);
//...
await wsProxyClient.TryDisconnectAsync();

//receive message [CLIENT 2]
Uri uri = new($"wss://localhost:7298/ws?groupName=group_1");  //always include a group name in the URL
WsProxyClient wsProxyClient = new(uri);
await wsProxyClient.TryConnectAsync();
wsProxyClient.On<SocketMessage>((socketMessage) => Console.WriteLine($"{socketMessage.Message}"));
wsProxyClient.On<byte[]>((data) => Console.WriteLine($"Bytes received. Length: {data.Length}"));
//...
await wsProxyClient.TryDisconnectAsync();
```

## Using the demo:

![](https://github.com/meehi/EasyWebSocketProxy/blob/main/client-to-client.gif)

1) Start Server (Middleware)
2) Start Client 2
3) Start Client 1 and follow the instructions

In the sample apps you can find my own Middleware implementation in ASP.NET Core project. It automatically handles client sessions and manages groups, basicly it's a forwarder:

## Server Usage:
```C#
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(120) });

app.UseWebSocketProxy("/ws");

app.MapGet("/", () => "Hello World!");

app.Run();
```
Or if you are using Razor pages:
```C#
// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(120) });  //Important
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseEndpoints(endpoints =>
    {
        endpoints.UseWebSocketProxy("/ws");  //Important
        endpoints.MapControllers();
        endpoints.MapRazorPages();
    });
}
```

If you are having trouble using the websocket server then please enable the Websocket Protocoll: https://learn.microsoft.com/en-us/iis/configuration/system.webserver/websocket
