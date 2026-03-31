using EasyNetworking.NetCore.Clients.WebSocketProxy;

namespace EasyNetworking.NetCore.Clients.MjpegStreamer
{
    public abstract class Streamer
    {
        private WsProxyClient? _wsProxyClient;
        private CancellationTokenSource? _streamCts;
        private readonly SemaphoreSlim _frameReady = new(0);
        private byte[]? _mjpegBuffer;

        public void InitStreamer(Uri streamEndPoint)
        {
            _streamCts = new();
            Task.Run(async () =>
            {
                _wsProxyClient = new WsProxyClient(streamEndPoint);
                if (await _wsProxyClient.ConnectAsync())
                {
                    try
                    {
                        while (!_streamCts.IsCancellationRequested)
                        {
                            await _frameReady.WaitAsync(_streamCts.Token);

                            if (_mjpegBuffer != null)
                            {
                                await _wsProxyClient.SendAsync(_mjpegBuffer);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }
            });
        }

        protected void SetNewFrame(byte[] buffer)
        {
            _mjpegBuffer = buffer;
            if (_frameReady.CurrentCount == 0)
                _frameReady.Release();
        }

        public void Close()
        {
            _streamCts?.Cancel();
            _wsProxyClient?.DisconnectAsync();
        }
    }
}