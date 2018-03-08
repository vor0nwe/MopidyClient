using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;

namespace MopidyTray
{
    class ExecuteEventArgs
    {
        public ExecuteEventArgs(string command, int commandID)
        {
            this.Command = command;
            this.CommandID = commandID;
        }
        public string Command { get; }
        public int CommandID { get; }
    }

    class EventEventArgs : CancelEventArgs
    {
        public EventEventArgs (string eventName, JToken data) : base(false)
        {
            this.EventName = eventName;
            this.Data = data;
        }
        public string EventName { get; }
        public JToken Data { get; }
    }

    class ResultEventArgs : CancelEventArgs
    {
        public ResultEventArgs(int commandID, JToken result) : base(false)
        {
            this.CommandID = commandID;
            this.Result = result;
        }
        public int CommandID { get; }
        public JToken Result { get; }
    }

    class ErrorEventArgs : CancelEventArgs
    {
        public ErrorEventArgs(int commandID, JToken error) : base(false)
        {
            this.CommandID = commandID;
            this.Error = error;
        }
        public int CommandID { get; }
        public JToken Error { get; }
    }

    class MopidyClient : IDisposable
    {
        private class CommandState
        {
            public Task Retriever;
            public JToken Result;
            public JToken Error;
        }

        private WebSocket _socket;
        private int _lastMessageID = (int)new Random().Next(int.MaxValue / 2);
        private Dictionary<int,CommandState> _commands = new Dictionary<int, CommandState>();

        public EventHandler                                 OnConnect { get; set; }
        public EventHandler<CloseEventArgs>                 OnDisconnect { get; set; }
        public EventHandler<WebSocketSharp.ErrorEventArgs>  OnError { get; set; }
        public EventHandler<MessageEventArgs>               OnMessage { get; set; }

        public EventHandler<EventEventArgs>                 OnEvent { get; set; }
        public EventHandler<ExecuteEventArgs>               OnExecute { get; set; }
        public EventHandler<ResultEventArgs>                OnCommandResult { get; set; }
        public EventHandler<ErrorEventArgs>                 OnCommandError { get; set; }

        public WebSocket WebSocket => _socket;
        public bool IsConnected => _socket.IsAlive;
        public string Uri => _socket.Url.ToString();

        public MopidyClient(string uri)
        {
            _socket = new WebSocket(uri);
            _socket.OnOpen += _socket_OnOpen;
            _socket.OnClose += _socket_OnClose;
            _socket.OnError += _socket_OnError;
            _socket.OnMessage += _socket_OnMessage;
        }

        /// <summary>
        /// Establishes a connection with the Mopidy server.
        /// </summary>
        public void Connect()
        {
            _socket.ConnectAsync();
        }

        /// <summary>
        /// Closes the connection with the Mopidy server.
        /// </summary>
        public void Disconnect()
        {
            _socket.CloseAsync();
        }

        /// <summary>
        /// Sends a command to the Mopidy server to execute.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="parameters">The parameters for the <paramref name="command"/>.</param>
        /// <returns>The ID of the command. Mopidy will include this ID in its response.</returns>
        public int Execute(string command, params object[] parameters)
        {
            var MessageID = ++_lastMessageID;
            dynamic Data = new { jsonrpc = "2.0", id = MessageID, method = command, @params = parameters };
            string Command = JsonConvert.SerializeObject(Data);
            _socket.SendAsync(Command, (sent) => {
                OnExecute?.Invoke(this, new ExecuteEventArgs(Command, MessageID));
            });
            
            // TODO: Log(Command, EventLogEntryType.Information);
            return MessageID;
        }

        /// <summary>
        /// Sends a command to the Mopidy server to execute, waits for the result, and returns that.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="parameters">The parameters for the <paramref name="command"/>.</param>
        /// <returns>Mopidy's result.</returns>
        public async Task<JToken> ExecuteAsync(string command, params object[] parameters)
        {
            // Send the command to the server
            var CommandID = Execute(command, parameters);

            // Prepare a task to retrieve the result, and add that task to the _commands dictionary
            var FetchResult = new Task<JToken>(FetchCommandResult, CommandID);
            Monitor.Enter(_commands);
            try
            {
                _commands.Add(CommandID, new CommandState { Retriever = FetchResult, Result = null });
            }
            finally
            {
                Monitor.Exit(_commands);
            }
            // Now wait for the task to complete (it will be started when the response comes in)
            var Result = await FetchResult;

            // Remove ID from _commandResults if we timed out
            if (Result == null)
            {
                Monitor.Enter(_commands);
                try
                {
                    if (_commands.ContainsKey(CommandID))
                        _commands.Remove(CommandID);
                }
                finally
                {
                    Monitor.Exit(_commands);
                }
            }

            return Result;
        }

        private JToken FetchCommandResult(object messageID)
        {
            Monitor.Enter(_commands);
            try
            {
                var CommandState = _commands[(int)messageID];
                _commands.Remove((int)messageID);
                return CommandState.Result;
            }
            finally
            {
                Monitor.Exit(_commands);
            }
        }

        private void _socket_OnMessage(object sender, MessageEventArgs e)
        {
            var DataToken = JToken.Parse(e.Data);
            Debug.Assert(DataToken.Type == JTokenType.Object, "Unexpected token type in message", "{0}", DataToken.Type.ToString());
            var Data = DataToken.Value<JObject>();
            if (Data.TryGetValue("event", out var Token) && OnEvent != null)
            {
                var ea = new EventEventArgs(Token.Value<string>(), DataToken);
                OnEvent(this, ea);
                if (ea.Cancel)
                    return;
            }
            else if (Data.TryGetValue("id", out var IDToken) && IDToken.Type == JTokenType.Integer)
            {
                int CommandID = IDToken.Value<int>();
                // check if we sent that CommandID; if so, handle the result; if not, trigger the appropriate event
                CommandState State;
                bool Found = false;
                Monitor.Enter(_commands);
                try
                {
                    Found = _commands.TryGetValue(CommandID, out State);
                }
                finally
                {
                    Monitor.Exit(_commands);
                }
                if (Data.TryGetValue("result", out var ResultToken))
                {
                    if (Found)
                    {
                        State.Result = ResultToken;
                        State.Retriever.Start();
                        return;
                    }
                    else if (OnCommandResult != null)
                    {
                        var ea = new ResultEventArgs(CommandID, ResultToken);
                        OnCommandResult(this, ea);
                        if (ea.Cancel)
                            return;
                    }
                }
                else if(Data.TryGetValue("error", out var ErrorToken))
                {
                    if (Found)
                    {
                        State.Error = ErrorToken;
                        State.Retriever.Start();
                        return;
                    }
                    else if (OnCommandError != null)
                    {
                        var ea = new ErrorEventArgs(CommandID, ErrorToken);
                        OnCommandError(this, ea);
                        if (ea.Cancel)
                            return;
                    }
                }
            }
            OnMessage?.Invoke(this, e);
        }

        private void _socket_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            OnError?.Invoke(this, e);
        }

        private void _socket_OnClose(object sender, CloseEventArgs e)
        {
            OnDisconnect?.Invoke(this, e);
        }

        private void _socket_OnOpen(object sender, EventArgs e)
        {
            OnConnect?.Invoke(this, e);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects).
                    Disconnect();
                    _commands.Clear();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MopidyClient() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
