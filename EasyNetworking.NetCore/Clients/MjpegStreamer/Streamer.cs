using EasyNetworking.NetCore.Clients.WebSocketProxy;

namespace EasyNetworking.NetCore.Clients.MjpegStreamer
{
    public abstract class Streamer
    {
        #region Private variables
        private WsProxyClient? _wsProxyClient;
        private CancellationTokenSource? _streamCts;
        #endregion

        #region Properties
        public bool HasNewFrame;
        public byte[]? MjpegBuffer;
        #endregion

        #region Public methods
        public void InitStreamer(Uri streamEndPoint)
        {
            _streamCts = new();

            Task.Run(async () =>
            {
                _wsProxyClient = new(streamEndPoint);
                if (await _wsProxyClient.TryConnectAsync())
                {
                    while (!_streamCts.IsCancellationRequested)
                    {
                        if (HasNewFrame)
                        {
                            await _wsProxyClient.SendAsync(MjpegBuffer!);
                            HasNewFrame = false;
                        }
                        Thread.Sleep(10);
                    }
                }
            });
        }

        public void Close()
        {
            _streamCts?.Cancel();
            _wsProxyClient?.TryDisconnectAsync();
        }
        #endregion
    }
}