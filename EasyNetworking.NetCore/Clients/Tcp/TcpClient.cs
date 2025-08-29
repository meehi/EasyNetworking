using System.Net;
using System.Text;
using Sockets = System.Net.Sockets;

namespace EasyNetworking.NetCore.Clients.Tcp
{
    public static class TcpClient
    {
        public static async Task<string> SendAsync(string host, int port)
        {
            using Sockets.TcpClient client = new();
            await client.ConnectAsync(IPAddress.Parse(host), port);
            await using Sockets.NetworkStream stream = client.GetStream();

            var buffer = new byte[1024];
            int received = await stream.ReadAsync(buffer);

            return Encoding.UTF8.GetString(buffer, 0, received);
        }
    }
}