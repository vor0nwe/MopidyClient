using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Text.RegularExpressions;
using WebSocketSharp;

namespace MopidyReport
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            WebSocket client = new WebSocket("ws://192.168.178.13:6680/mopidy/ws");
            client.OnOpen += Client_OnOpen;
            client.OnError += Client_OnError;
            client.OnMessage += Client_OnMessage;
            client.OnClose += Client_OnClose;

            int msgID = 0;
            client.Connect();
            try
            {
                while (client.IsAlive)
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
                            dynamic parameters = JsonConvert.DeserializeObject("[" + match.Groups[2].Value + "]");
                            data = new { jsonrpc = "2.0", id = ++msgID, method = match.Groups[1].Value, @params = parameters };
                        }
                        else
                        {
                            data = new { jsonrpc = "2.0", id = ++msgID, method = match.Groups[1].Value };
                        }
                        msg = JsonConvert.SerializeObject(data);
                        PrefixTime(msg, ConsoleColor.DarkGray, ConsoleColor.DarkYellow);
                    }
                    client.Send(msg);
                }
            }
            finally
            {
                if (client.IsAlive)
                    client.Close();
            }
            Console.ReadKey(true);
        }

        private static ConsoleColor PrefixTime(string text, ConsoleColor color, ConsoleColor prefixColor = ConsoleColor.Yellow)
        {
            var OldColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = prefixColor;
                Console.Write(DateTime.Now.ToString("HH:mm:ss.fff") + " ");
                Console.ForegroundColor = color;
                Console.WriteLine(text);
                return OldColor;
            }
            finally
            {
                Console.ForegroundColor = OldColor;
            }
        }

        private static void Client_OnClose(object sender, CloseEventArgs e)
        {
            PrefixTime("Connection closed", ConsoleColor.Cyan);
        }

        private static void Client_OnMessage(object sender, MessageEventArgs e)
        {
            var OldColor = PrefixTime(e.Data, ConsoleColor.DarkGray);
            try
            {
                Console.ForegroundColor = ConsoleColor.Gray;

                dynamic data = JsonConvert.DeserializeObject(e.Data);

                if (data.result != null)
                {
                    string result = JsonConvert.SerializeObject(data.result, Formatting.Indented);
                    string trackUri = null;
                    if (data.result.uri != null)
                        trackUri = data.result.uri;
                    else if (data.result.track?.uri != null)
                        trackUri = data.result.track.uri;
                    if (!string.IsNullOrEmpty(trackUri))
                        result += "\n" + Uri.UnescapeDataString(trackUri);
                    Console.WriteLine(result);
                }
                else if (data.error != null)
                {
                    throw new Exception(JsonConvert.SerializeObject(data.error, Formatting.Indented));
                }
                else
                {
                    Console.WriteLine(JsonConvert.SerializeObject(data, Formatting.Indented));
                    string trackUri = null;
                    if (data.uri != null)
                        trackUri = data.uri;
                    else if (data.tl_track != null)
                        trackUri = data.tl_track.track.uri;
                    if (!string.IsNullOrEmpty(trackUri))
                        Console.WriteLine(Uri.UnescapeDataString(trackUri));
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
            PrefixTime(e.Message, ConsoleColor.Red);
        }

        private static void Client_OnOpen(object sender, EventArgs e)
        {
            PrefixTime("Connection open", ConsoleColor.Cyan);
        }

    }
}
