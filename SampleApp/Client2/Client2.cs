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
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("Something went wrong. Good bye!");
                Console.ReadLine();
                return;
            }

            wsProxyClient.On<SocketMessage>(async (socketMessage) =>
            {
                if (!socketMessage.ReplyRequired && !socketMessage.ReplySimpleString)
                    Console.WriteLine($"1) Type message received. Message: {socketMessage.Message}");

                if (socketMessage.ReplyRequired)
                {
                    Console.WriteLine();
                    Console.WriteLine("3) Sending type message as answer.");
                    await wsProxyClient.ReplyToAsync(socketMessage, new SocketMessage() { Message = "This is my answer from Client 2!" });
                }
                if (socketMessage.ReplySimpleString)
                {
                    Console.WriteLine();
                    Console.WriteLine("6) Sending simple text answer.");
                    await wsProxyClient.ReplyToAsync(socketMessage, "Simple text answer from Client 2.");
                }
            });
            wsProxyClient.On<byte[]>((data) =>
            {
                Console.WriteLine();
                Console.WriteLine($"2) Bytes received. Length: {data.Length}");
            });
            wsProxyClient.On<string>(async (question) =>
            {
                Console.WriteLine();
                Console.WriteLine($"4) {question}. Sending answer True.");
                await wsProxyClient.ReplyToAsync(question, true);
            });
            wsProxyClient.On<List<string>>((lines) =>
            {
                Console.WriteLine();
                Console.WriteLine("5) List of strings received:");
                foreach (string line in lines)
                    Console.WriteLine(line);
            });

            Console.ReadLine();
            await wsProxyClient.TryDisconnectAsync();
        }
    }
}