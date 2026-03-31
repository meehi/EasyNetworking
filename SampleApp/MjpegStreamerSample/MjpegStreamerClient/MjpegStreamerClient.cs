using MjpegStreamerClient;
using SharedData.Models;
using System.Diagnostics;

var uri = new Uri($"{EndPoints.WSS_HOST}/{EndPoints.STREAM}?groupName={EndPoints.GROUP_NAME}");

var cam = new WebCamStreamer();
cam.Start(uri, camIndex: 0, quality: 90);

await Task.Delay(2000);

Process.Start(new ProcessStartInfo() { UseShellExecute = true, FileName = $"{EndPoints.HTTPS_HOST}/{EndPoints.LIVE}?groupName={EndPoints.GROUP_NAME}" });

Console.WriteLine("Press ENTER to stop...");
Console.ReadLine();

cam.Stop();
cam.Close();