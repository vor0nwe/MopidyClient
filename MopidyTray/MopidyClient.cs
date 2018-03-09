using MopidyTray.Models;
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
                _commands.Add(CommandID, new CommandState {
                    Retriever = FetchResult,
                    Result = null,
                    Error = null,
                });
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

        public async Task<T> ExecuteAsync<T>(string command, params object[] parameters)
        {
            var result = await ExecuteAsync(command, parameters);
            return result.ToObject<T>();
        }

        public T Execute<T>(string command, params object[] parameters)
        {
            return Sync(ExecuteAsync<T>(command, parameters));
        }

        public T Sync<T>(Task<T> task)
        {
            task.Start();
            task.Wait();
            return task.Result;
        }
        public void Sync(Task task)
        {
            task.Start();
            task.Wait();
            return;
        }


        private JToken FetchCommandResult(object messageID)
        {
            Monitor.Enter(_commands);
            try
            {
                var CommandState = _commands[(int)messageID];
                _commands.Remove((int)messageID);
                if (CommandState.Error != null)
                    // TODO: make custom exception type
                    throw new Exception(CommandState.Error.ToString());
                else
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
            if (Data.TryGetValue("event", out var EventToken) && OnEvent != null)
            {
                var ea = new EventEventArgs(EventToken.Value<string>(), DataToken);
                EventToken.Remove();
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


        public enum PlaybackState
        {
            stopped,
            playing,
            paused
        }

        public PlaybackState State
        {
            get => Sync(GetStateAsync());
            set => Sync(SetStateAsync(value));
        }
        public async Task<PlaybackState> GetStateAsync()
        {
            var result = await ExecuteAsync<string>("core.playback.get_state");
            return (PlaybackState)Enum.Parse(typeof(PlaybackState), result);
        }
        public async Task SetStateAsync(PlaybackState value)
        {
            await ExecuteAsync<bool?>("core.playback.set_state", value.ToString());
        }

        public bool Muted
        {
            get => Sync(GetMutedAsync());
            set => Sync(SetMutedAsync(value));
        }
        public async Task<bool> GetMutedAsync()
        {
            return await ExecuteAsync<bool>("core.mixer.get_mute");
        }
        public async Task SetMutedAsync(bool value)
        {
            await ExecuteAsync<bool?>("core.mixer.set_mute", value);
        }

        public int Volume
        {
            get => Sync(GetVolumeAsync());
            set => Sync(SetVolumeAsync(value));
        }
        public async Task<int> GetVolumeAsync()
        {
            return await ExecuteAsync<int>("core.mixer.get_volume");
        }
        public async Task SetVolumeAsync(int value)
        {
            await ExecuteAsync<bool?>("core.mixer.set_volume", value);
        }

        public void Play(TlTrack track = null, int? tlid = null)
        {
            Sync(PlayAsync(track, tlid));
        }
        public async Task PlayAsync(TlTrack track = null, int? tlid = null)
        {
            await ExecuteAsync<bool?>("core.playback.play", track, tlid);
        }

        public void Next()
        {
            Sync(NextAsync());
        }
        public async Task NextAsync()
        {
            await ExecuteAsync<bool?>("core.playback.next");
        }

        public void Previous()
        {
            Sync(PreviousAsync());
        }
        public async Task PreviousAsync()
        {
            await ExecuteAsync<bool?>("core.playback.previous");
        }

        public void Stop()
        {
            Sync(StopAsync());
        }
        public async Task StopAsync()
        {
            await ExecuteAsync<bool?>("core.playback.stop");
        }

        public void Pause()
        {
            Sync(PauseAsync());
        }
        public async Task PauseAsync()
        {
            await ExecuteAsync<bool?>("core.playback.pause");
        }

        public void Resume()
        {
            Sync(ResumeAsync());
        }
        public async Task ResumeAsync()
        {
            await ExecuteAsync<bool?>("core.playback.resume");
        }

        public bool Seek(uint timePosition) 
        {
            return Sync(SeekAsync(timePosition));
        }
        public async Task<bool> SeekAsync(uint timePosition)
        {
            return await ExecuteAsync<bool>("core.playback.seek", timePosition);
        }

        public TlTrack GetCurrentTlTrack()
        {
            return Sync(GetCurrentTlTrackAsync());
        }
        public async Task<TlTrack> GetCurrentTlTrackAsync()
        {
            return await ExecuteAsync<TlTrack>("core.playback.get_current_tl_track");
        }

        public Track GetCurrentTrack()
        {
            return Sync(GetCurrentTrackAsync());
        }
        public async Task<Track> GetCurrentTrackAsync()
        {
            return await ExecuteAsync<Track>("core.playback.get_current_track");
        }

        public string GetStreamTitle()
        {
            return Sync(GetStreamTitleAsync());
        }
        public async Task<string> GetStreamTitleAsync()
        {
            return await ExecuteAsync<string>("core.playback.get_stream_title");
        }

        public uint GetTimePosition()
        {
            return Sync(GetTimePositionAsync());
        }
        public async Task<uint> GetTimePositionAsync()
        {
            return await ExecuteAsync<uint>("core.playback.get_time_position");
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
