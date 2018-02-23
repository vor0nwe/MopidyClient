using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MopidyReport
{
    class Program
    {
        static void Main(string[] args)
        {
            var clientTask = Client();
            clientTask.Wait();
            // Console.ReadLine();
        }

        static async Task Client()
        {
            using (ClientWebSocket ws = new ClientWebSocket())
            {
                Uri serverUri = new Uri("ws://192.168.178.13:6680/mopidy/ws");
                await ws.ConnectAsync(serverUri, CancellationToken.None);
                while (true)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("Input message ('exit' to exit): ");
                    Console.ForegroundColor = ConsoleColor.White;
                    string msg = Console.ReadLine();
                    Console.ForegroundColor = ConsoleColor.Gray;
                    if (msg == "exit")
                    {
                        break;
                    }
                    ArraySegment<byte> bytesToSend = new ArraySegment<byte>(
                        Encoding.UTF8.GetBytes(msg));
                    await ws.SendAsync(
                        bytesToSend, WebSocketMessageType.Text,
                        true, CancellationToken.None);
                    ArraySegment<byte> bytesReceived = new ArraySegment<byte>(new byte[1024]);
                    WebSocketReceiveResult result = await ws.ReceiveAsync(
                        bytesReceived, CancellationToken.None);
                    Console.WriteLine(Encoding.UTF8.GetString(
                        bytesReceived.Array, 0, result.Count));
                    if (ws.State != WebSocketState.Open)
                    {
                        break;
                    }
                }
            }

        }
    }
}
