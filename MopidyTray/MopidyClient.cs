using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace MopidyTray
{
    class EventEventArgs : EventArgs
    {
        public EventEventArgs (string eventName, dynamic data) : base()
        {
            this.EventName = eventName;
            this.Data = data;
        }
        public string EventName { get; }
        public dynamic Data { get; }
    }

    class MopidyClient
    {
        private WebSocket _socket;
        private uint _messageID = (uint)new Random().Next(int.MaxValue);

        public EventHandler                     OnConnect { get; set; }
        public EventHandler<CloseEventArgs>     OnDisconnect { get; set; }
        public EventHandler<ErrorEventArgs>     OnError { get; set; }
        public EventHandler<MessageEventArgs>   OnMessage { get; set; }
        public EventHandler<EventEventArgs>     OnEvent { get; set; }

        public MopidyClient(Uri uri)
        {
            _socket = new WebSocket(uri.ToString());
            _socket.OnOpen += _socket_OnOpen;
            _socket.OnClose += _socket_OnClose;
            _socket.OnError += _socket_OnError;
            _socket.OnMessage += _socket_OnMessage;
        }

        public void Connect()
        {
            _socket.ConnectAsync();
        }

        public void Disconnect()
        {
            _socket.CloseAsync();
        }

        public uint Execute(string command, params object[] parameters)
        {
            var MessageID = ++_messageID;
            dynamic Data = new { jsonrpc = "2.0", id = MessageID, method = command, @params = parameters };
            string Command = JsonConvert.SerializeObject(Data);
            _socket.Send(Command);
            //Log(Command, EventLogEntryType.Information);
            return MessageID;
        }

        private void _socket_OnMessage(object sender, MessageEventArgs e)
        {
            // TODO: perform our own message handling; only if that doesn't do anything, fall through
            this.OnMessage(this, e);
        }

        private void _socket_OnError(object sender, ErrorEventArgs e)
        {
            this.OnError(this, e);
        }

        private void _socket_OnClose(object sender, CloseEventArgs e)
        {
            this.OnDisconnect(this, e);
        }

        private void _socket_OnOpen(object sender, EventArgs e)
        {
            this.OnConnect(this, e);
        }
    }
}
