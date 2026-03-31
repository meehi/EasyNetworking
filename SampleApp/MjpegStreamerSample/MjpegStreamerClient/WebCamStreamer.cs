using EasyNetworking.NetCore.Clients.MjpegStreamer;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using SharedData.Models;

namespace MjpegStreamerClient
{
    public class WebCamStreamer : Streamer
    {
        private VideoCapture? _capture;
        private readonly VectorOfByte _compressionBuffer = new();
        private Image<Bgr, byte>? _frameBuffer;
        private KeyValuePair<ImwriteFlags, int>[]? _compressionParams;
        private DateTime _lastFrameTime = DateTime.MinValue;
        private readonly TimeSpan _frameInterval = TimeSpan.FromMilliseconds(40); // ~25 FPS

        public void Start(Uri uri, int camIndex = 0, int quality = 80)
        {
            _compressionParams = [new(ImwriteFlags.JpegQuality, quality)];
            _frameBuffer = new Image<Bgr, byte>(1280, 720);

            InitStreamer(uri);

            Task.Run(() =>
            {
                try
                {
                    _capture = new VideoCapture(camIndex, VideoCapture.API.DShow,
                    [
                        new(CapProp.FrameWidth, 1280),
                        new(CapProp.FrameHeight, 720)
                    ]);

                    _capture.ImageGrabbed += OnImageGrabbed;
                    _capture.Start();

                    Console.WriteLine($"Streaming started on {uri} using DirectShow...");
                    Console.WriteLine($"View live stream in your browser: {EndPoints.HTTPS_HOST}/{EndPoints.LIVE}?groupName={EndPoints.GROUP_NAME}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error starting capture: {ex.Message}");
                }
            });
        }

        private void OnImageGrabbed(object? sender, EventArgs e)
        {
            if (_capture == null || _frameBuffer == null)
                return;

            var now = DateTime.UtcNow;
            if (now - _lastFrameTime < _frameInterval)
                return;
            _lastFrameTime = now;

            try
            {
                if (_capture.Retrieve(_frameBuffer))
                {
                    CvInvoke.Imencode(".jpg", _frameBuffer, _compressionBuffer, _compressionParams);
                    SetNewFrame(_compressionBuffer.ToArray());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Grab error: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (_capture != null)
            {
                _capture.Stop();
                _capture.ImageGrabbed -= OnImageGrabbed;
                _capture.Dispose();
                _capture = null;
            }

            _frameBuffer?.Dispose();
            _frameBuffer = null;
            _compressionBuffer.Clear();
            _compressionBuffer.Dispose();

            Console.WriteLine("Streaming stopped and resources released.");
        }
    }
}