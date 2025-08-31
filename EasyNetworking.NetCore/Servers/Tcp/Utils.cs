using System.Net;
using System.Net.Sockets;

namespace EasyNetworking.NetCore.Servers.Tcp
{
    public static class Utils
    {
        public static IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var address in host.AddressList)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    return address;
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
    }
}