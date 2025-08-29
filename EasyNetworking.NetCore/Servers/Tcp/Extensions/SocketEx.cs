using System.Net.Sockets;
using System.Text;

namespace EasyNetworking.NetCore.Servers.Tcp.Extensions
{
    internal static class SocketEx
    {
        public static IEnumerable<Socket> IncommingConnections(this Socket server)
        {
            while (true)
            {
                yield return server.Accept();
            }
        }

        public static async Task ReplyAsync(this Socket client, string message)
        {
            await client.SendAsync(Encoding.UTF8.GetBytes(message));
        }
    }
}