// See https://aka.ms/new-console-template for more information
using EasyNetworking.NetCore.Clients.WebSocketProxy;
using SharedData.Models;

namespace Client2
{
    public class Client2
    {
        static async Task Main()
        {
            Console.WriteLine("Hello, I'm client 2! Please make sure that Server and the other Client is running!");

            Uri uri = new($"{Common.HOST}?groupName={Common.GROUP_NAME}");

            WsProxyClient wsProxyClient = new(uri);
            Console.WriteLine($"Connecting to {Common.HOST} with the following group name: {Common.GROUP_NAME}");
            if (await wsProxyClient.TryConnectAsync())
            {
                Console.WriteLine("Connected!");
                Console.WriteLine("Waiting for receiving messages from Client 1...");
            }
            else
            {
                Console.WriteLine("Something went wrong. Good bye!");
                Console.ReadLine();
                return;
            }

            wsProxyClient.On<SocketMessage>(async (socketMessage) =>
            {
                Console.WriteLine($"Type message received. Message: {socketMessage.Message}");
                if (socketMessage.ReplyRequired)
                    await wsProxyClient.ReplyToAsync(socketMessage, new SocketMessage() { Message = "This is my answer from Client 2!" });
            });
            wsProxyClient.On<byte[]>((data) => Console.WriteLine($"Bytes received. Length: {data.Length}"));
            wsProxyClient.On<string>(async (question) =>
            {
                Console.WriteLine($"{question}");
                await wsProxyClient.ReplyToAsync(question, true);
            });
            wsProxyClient.On<List<string>>((lines) =>
            {
                foreach (string line in lines)
                    Console.WriteLine(line);
            });

            Console.ReadLine();
            await wsProxyClient.TryDisconnectAsync();
        }
    }
}