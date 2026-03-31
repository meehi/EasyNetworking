using System.Text;

namespace EasyNetworking.AspNetCore.Servers.MjpegStreamer.Models
{
    internal class MjpegWriter(Stream stream, string boundary = "--boundary")
    {
        private readonly Stream _stream = stream;
        private readonly string _boundary = boundary;

        public async Task WriteFrameAsync(byte[] frame, CancellationToken ct)
        {
            string header = $"\r\n{_boundary}\r\nContent-Type: image/jpeg\r\nContent-Length: {frame.Length}\r\n\r\n";

            await _stream.WriteAsync(Encoding.ASCII.GetBytes(header), ct);
            await _stream.WriteAsync(frame, ct);
            await _stream.FlushAsync(ct);
        }
    }
}