// See https://aka.ms/new-console-template for more information
using EasyNetworking.NetCore.Clients.WebSocketProxy;
using SharedData.Models;
using System.Text;

namespace Client1
{
    public class Client1
    {
        static async Task Main()
        {
            Console.WriteLine("Hello, I'm client 1! Please make sure that Server and the other Client is running!");
            Console.WriteLine("Press enter to continue...");
            Console.ReadLine();

            Uri uri = new($"{Common.HOST}?groupName={Common.GROUP_NAME}");

            WsProxyClient wsProxyClient = new(uri);
            Console.WriteLine($"Connecting to {Common.HOST} with the following group name: {Common.GROUP_NAME}");
            if (await wsProxyClient.TryConnectAsync())
                Console.WriteLine("Connected!");
            else
            {
                Console.WriteLine("Something went wrong. Good bye!");
                Console.ReadLine();
                return;
            }

            Console.WriteLine();
            Console.Write("1) Press enter to send Type message to Client 2...");
            Console.ReadLine();
            await wsProxyClient.SendAsync<SocketMessage>(new() { Message = "Hello from Client 1" });
            Console.WriteLine("Type message sent!");

            Console.WriteLine();
            Console.Write("2) Press enter to send byte array to Client 2...");
            Console.ReadLine();
            byte[] data = Encoding.UTF8.GetBytes("Hello from Client 1! This is going to be a byte array message!");
            await wsProxyClient.SendAsync(data);
            Console.WriteLine("Byte array sent!");

            Console.WriteLine();
            Console.Write("3) Press enter to send Type message and wait for the answer...");
            Console.ReadLine();
            SocketMessage? socketMessageResponse = await wsProxyClient.SendAsync<SocketMessage, SocketMessage>(new()
            {
                Message = "This message is waiting for an answer!",
                ReplyRequired = true
            }, new CancellationTokenSource(TimeSpan.FromSeconds(10)));
            if (socketMessageResponse != null)
                Console.WriteLine(socketMessageResponse.Message);

            Console.WriteLine();
            Console.Write("4) Press enter to ask a question from Client 2...");
            Console.ReadLine();
            bool response = await wsProxyClient.SendAsync<string, bool>("Is it True or False?", new CancellationTokenSource(TimeSpan.FromSeconds(10)));
            Console.WriteLine($"The answer is {response}");

            Console.WriteLine();
            Console.Write("5) Press enter to send a list of string to Client 2...");
            Console.ReadLine();
            await wsProxyClient.SendAsync<List<string>>(["Line 1", "Line 2"]);
            Console.WriteLine("List of strings sent!");

            Console.WriteLine();
            Console.Write("6) Press enter to send and receive simple text message to Client 2...");
            Console.ReadLine();
            string? result = await wsProxyClient.SendAsync<SocketMessage, string>(new() { Message = "Hello", ReplySimpleString = true });
            Console.WriteLine(result);

            Console.ReadLine();
            await wsProxyClient.TryDisconnectAsync();
        }
    }
}