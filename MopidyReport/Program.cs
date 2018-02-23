﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
//using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;

namespace MopidyReport
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            //var clientTask = Client();
            //clientTask.Wait();
            // Console.ReadLine();

            WebSocket client = new WebSocket("ws://192.168.178.13:6680/mopidy/ws");
            client.OnOpen += Client_OnOpen;
            client.OnError += Client_OnError;
            client.OnMessage += Client_OnMessage;
            client.OnClose += Client_OnClose;

            int msgID = 0;
            client.Connect();
            try
            {
                while (true)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    string msg = Console.ReadLine();
                    if (msg == "exit")
                        break;
                    var match = Regex.Match(msg, @"([a-z_.]+)\s*(?:\((.*)\))?", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        dynamic data;
                        if (match.Groups[2].Success)
                        {
                            dynamic parameters =  JsonConvert.DeserializeObject("[" + match.Groups[2].Value + "]");
                            data = new { jsonrpc = "2.0", id = ++msgID, method = match.Groups[1].Value, @params = parameters };
                        }
                        else
                        {
                            data = new { jsonrpc = "2.0", id = ++msgID, method = match.Groups[1].Value };
                        }


                        //dynamic data = new { jsonrpc = "2.0", id = ++msgID, method = match.Groups[1].Value, @params = new List<dynamic>() };
                        //if (match.Groups[2].Success /*&& match.Groups[2].Value.Length > 0*/)
                        //{
                        //    string[] parameters = match.Groups[2].Value.Split(new char[] { ',' });
                        //    if (parameters.Length == 1 && parameters[0] == "")
                        //        data.@params.Add(null);
                        //    else
                        //        foreach (string param in parameters)
                        //            data.@params.Add(param);
                        //}
                        msg = JsonConvert.SerializeObject(data);
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine(msg);
                    }
                    client.Send(msg);
                }
            }
            finally
            {
                client.Close();
            }
        }

        private static ConsoleColor PrefixTime(ConsoleColor color)
        {
            var OldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(DateTime.Now.ToString("HH:mm:ss.fff") + " ");
            Console.ForegroundColor = color;
            return OldColor;
        }

        private static void Client_OnClose(object sender, CloseEventArgs e)
        {
            var OldColor = PrefixTime(ConsoleColor.Cyan);
            Console.WriteLine("Connection closed");
            Console.ForegroundColor = OldColor;
        }

        private static void Client_OnMessage(object sender, MessageEventArgs e)
        {
            var OldColor = PrefixTime(ConsoleColor.DarkGray);
            try
            {
                Console.WriteLine(e.Data);
                Console.ForegroundColor = ConsoleColor.Gray;
                dynamic data = JsonConvert.DeserializeObject(e.Data);
                if (data.result != null)
                {
                    string result;
                    if (data.result.GetType() == typeof(string))
                    {
                        result = data.result;
                    }
                    else
                    {
                        result = JsonConvert.SerializeObject(data.result, Formatting.Indented);
                    }
                    Console.WriteLine(result);
                }
                else if (data.error != null)
                {
                    throw new Exception(JsonConvert.SerializeObject(data.error, Formatting.Indented));
                }
                else
                {
                    Console.WriteLine(JsonConvert.SerializeObject(data, Formatting.Indented));
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Error.WriteLine(ex.Message);
            }
            finally
            {
                Console.ForegroundColor = OldColor;
            }
        }

        private static void Client_OnError(object sender, ErrorEventArgs e)
        {
            var OldColor = PrefixTime(ConsoleColor.Red);
            Console.Error.WriteLine(e.Message);
            Console.ForegroundColor = OldColor;
        }

        private static void Client_OnOpen(object sender, EventArgs e)
        {
            var OldColor = PrefixTime(ConsoleColor.Cyan);
            Console.WriteLine("Connection open");
            Console.ForegroundColor = OldColor;
        }

        //static async Task Client()
        //{
        //    using (ClientWebSocket ws = new ClientWebSocket())
        //    {
        //        Uri serverUri = new Uri("ws://192.168.178.13:6680/mopidy/ws");
        //        await ws.ConnectAsync(serverUri, CancellationToken.None);
        //        while (true)
        //        {
        //            Console.ForegroundColor = ConsoleColor.Green;
        //            Console.Write("Input message ('exit' to exit): ");
        //            Console.ForegroundColor = ConsoleColor.White;
        //            string msg = Console.ReadLine();
        //            Console.ForegroundColor = ConsoleColor.Gray;
        //            if (msg == "exit")
        //            {
        //                break;
        //            }
        //            ArraySegment<byte> bytesToSend = new ArraySegment<byte>(
        //                Encoding.UTF8.GetBytes(msg));
        //            await ws.SendAsync(
        //                bytesToSend, WebSocketMessageType.Text,
        //                true, CancellationToken.None);
        //            ArraySegment<byte> bytesReceived = new ArraySegment<byte>(new byte[1024]);
        //            WebSocketReceiveResult result = await ws.ReceiveAsync(
        //                bytesReceived, CancellationToken.None);
        //            Console.WriteLine(Encoding.UTF8.GetString(
        //                bytesReceived.Array, 0, result.Count));
        //            if (ws.State != WebSocketState.Open)
        //            {
        //                break;
        //            }
        //        }
        //    }
        //}
    }
}
