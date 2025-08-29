using Microsoft.AspNetCore.Http;

namespace EasyNetworking.AspNetCore.Servers.MjpegStreamer.Models
{
    internal class Client
    {
        public string? Id { get; set; }
        public MjpegWriter? MjpegWriter { get; set; }
        public HttpContext? Context { get; set; }
        public HttpResponse? Response { get; set; }
        public CancellationTokenSource? StreamingCts { get; set; }
    }
}