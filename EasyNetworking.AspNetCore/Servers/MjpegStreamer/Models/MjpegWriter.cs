using System.Text;

namespace EasyNetworking.AspNetCore.Servers.MjpegStreamer.Models
{
    internal class MjpegWriter(Stream stream, string boundary) : IDisposable
    {
        public MjpegWriter(Stream stream) : this(stream, "--boundary") { }

        public string Boundary { get; private set; } = boundary;
        public Stream? Stream { get; private set; } = stream;

        public async Task WriteAsync(byte[] imageBytes)
        {
            if (Stream == null)
                return;

            string header =
               $"{Boundary}\r\n" +
               $"Content-Type: image/jpeg\r\n" +
               $"Content-Length: {imageBytes.Length}\r\n\r\n";

            await Stream.WriteAsync(Encoding.ASCII.GetBytes(header));

            await Stream.WriteAsync(imageBytes);

            await Stream.WriteAsync(Encoding.ASCII.GetBytes("\r\n"));

            await Stream.FlushAsync();
        }

        public void Dispose()
        {
            try
            {
                Stream?.Dispose();
            }
            finally
            {
                Stream = null;
            }
        }
    }
}