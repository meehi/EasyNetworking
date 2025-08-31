using EasyNetworking.NetCore.Servers.Tcp.Extensions;
using System.Net;
using System.Net.Sockets;

namespace EasyNetworking.NetCore.Servers.Tcp
{
    public class TcpServer
    {
        #region Events
        public delegate void ClientConnectedEventArgs(Socket client);
        public event ClientConnectedEventArgs? OnClientConnected;
        #endregion

        #region Private variables
        private Thread? _serverThread;
        private Socket? _serverSocket;
        private List<Socket>? _clientSockets;
        #endregion

        #region Public methods
        public void StartListening(int port)
        {
            _clientSockets = [];
            lock (this)
            {
                _serverThread = new(new ParameterizedThreadStart(ServerThread))
                {
                    IsBackground = true
                };
                _serverThread.Start(port);
            }
        }

        public void StopListening()
        {
            if (_serverThread != null && _serverThread.IsAlive)
            {
                try
                {
                    lock (_clientSockets!)
                    {
                        foreach (var client in _clientSockets)
                            client.Close();
                        _clientSockets.Clear();
                    }
                    _serverSocket!.Close();
                    _serverThread = null;
                    _serverSocket = null;
                }
                catch
                {
                }
            }
        }
        #endregion

        #region Private methods
        private void ServerThread(object? port)
        {
            try
            {
                _serverSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _serverSocket.Bind(new IPEndPoint(Utils.GetLocalIPAddress(), (int)port!));
                _serverSocket.Listen(100);

                foreach (Socket client in _serverSocket.IncommingConnections())
                {
                    lock (_clientSockets!)
                    {
                        if (!_clientSockets.Contains(client))
                        {
                            _clientSockets.Add(client);
                            OnClientConnected?.Invoke(client);
                        }
                    }
                }
            }
            catch
            {
            }
        }
        #endregion
    }
}