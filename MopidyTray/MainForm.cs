using MopidyTray.Models;
using MopidyTray.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MopidyTray
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private MopidyClient Mopidy;
        private TrayIcon trayIcon;

        private async void MainForm_Load(object sender, EventArgs e)
        {
            trayIcon = new TrayIcon(this, this.Text, Resources.mopidy_icon_gray);

            imageList.Images.Add(SystemIcons.Error);
            imageList.Images.Add(SystemIcons.Warning);
            imageList.Images.Add(SystemIcons.Information);
            imageList.Images.Add(SystemIcons.Application);
            imageList.Images.Add(SystemIcons.Question);

            if (!Settings.Default.URIConfirmed)
            {
                ConfirmURI();
            }

            ReconnectMopidy(Settings.Default.HostUri);

            var prop = new SettingsProperty("Commands")
            {
                PropertyType = typeof(string),
                DefaultValue = Settings.Default.Command,
                Provider = Settings.Default.Providers["LocalFileSettingsProvider"],
                SerializeAs = SettingsSerializeAs.String
            };
            prop.Attributes.Add(typeof(UserScopedSettingAttribute), new UserScopedSettingAttribute());
            Settings.Default.Properties.Add(prop);
            Settings.Default.Reload();

            string Commands = (string)Settings.Default["Commands"];
            if (!string.IsNullOrWhiteSpace(Commands))
                foreach (string Command in Commands.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                {
                    comboCommand.Items.Add(Command);
                }

            string state = await Mopidy.ExecuteAsync<string>("core.playback.get_state");
            SetProperty("State", state);
            if (state == "playing" || state == "paused")
            {
                var Track = await Mopidy.GetCurrentTlTrackAsync();
                DescribeTrack(Track, true);
                var TimePosition = await Mopidy.GetTimePositionAsync();
                UpdatePosition(Track.Track.Length, TimePosition, state == "playing");
            }
            
        }

        private void ConfirmURI()
        {
            var InputBox = new Form
            {
                Width = 500,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = this.Text,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                Font = this.Font
            };
            Label textLabel = new Label() { Left = 10, Top = 10, Text = "Mopidy URI:" };
            TextBox textBox = new TextBox() { Left = 10, Top = textLabel.Bottom + 10, Width = InputBox.ClientSize.Width - 20, Text = Settings.Default.HostUri };
            Button confirmation = new Button() { Text = "OK", Left = InputBox.ClientSize.Width - 220, Width = 100, Top = textBox.Bottom + 10, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = "Cancel", Left = InputBox.ClientSize.Width - 110, Width = 100, Top = textBox.Bottom + 10, DialogResult = DialogResult.Cancel };
            confirmation.Click += (lsender, le) => { InputBox.Close(); };
            cancel.Click += (lsender, le) => { InputBox.Close(); };
            InputBox.Controls.Add(textBox);
            InputBox.Controls.Add(confirmation);
            InputBox.Controls.Add(cancel);
            InputBox.Controls.Add(textLabel);
            InputBox.AcceptButton = confirmation;
            InputBox.CancelButton = cancel;

            if (InputBox.ShowDialog() == DialogResult.OK)
            {
                Settings.Default.HostUri = textBox.Text;
                Settings.Default.URIConfirmed = true;
                Settings.Default.Save();
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (Mopidy.IsConnected)
                Mopidy.Disconnect();
            
            trayIcon.Dispose();
            trayIcon = null;

            var Commands = new List<string>();
            foreach (string Item in comboCommand.Items)
            {
                Commands.Add(Item);
            }
            Commands.Sort();
            Settings.Default["Commands"] = string.Join(Environment.NewLine, Commands);
            Settings.Default.MainForm_Location = this.Location;
            Settings.Default.Save();
        }

        private void DescribeTrack(Models.TlTrack tl_track, bool showNotification)
        {
            DescribeTrack(tl_track.Track, showNotification);
        }
        private void DescribeTrack(Models.Track track, bool showNotification)
        {
            var orgTrackInfo = TrackInfo.FromTrack(track, true);
            var trackInfo = TrackInfo.FromTrack(track, false);
            state.Invoke((MethodInvoker)delegate
            {
                state.BeginUpdate();
                try
                {
                    string uri = track.Uri;
                    uri = Uri.UnescapeDataString(uri);
                    SetProperty("Track URI", uri);
                    SetProperty("Track Title", orgTrackInfo.Title, true);
                    SetProperty("Track Artist(s)", orgTrackInfo.Artists);
                    SetProperty("Track Album", orgTrackInfo.Album);
                }
                finally
                {
                    state.EndUpdate();
                }

                if (showNotification)
                {
                    string Description = trackInfo.Artists;
                    if (!string.IsNullOrWhiteSpace(trackInfo.Album))
                        Description += Environment.NewLine + "(" + trackInfo.Album + ")";
                    trayIcon.ShowNotification(trackInfo.Title, Description.Trim(), MessageBoxIcon.None, true);
                }
                var TrackLine = $"{trackInfo.Title} - {trackInfo.Artists}";
                this.Text = TrackLine + " - " + Application.ProductName;
                if (TrackLine.Length > 63)
                    trayIcon.Text = TrackLine.Substring(0, 62) + "…";
                else
                    trayIcon.Text = TrackLine;
            });
        }

        private void Client_OnMessage(object sender, WebSocketSharp.MessageEventArgs e)
        {
            try
            {
                var jToken = JToken.Parse(e.Data);
                Debug.Assert(jToken.Type == JTokenType.Object, $"Unexpected token type: {jToken.Type}");
                var jData = (JObject)jToken;
                dynamic data = JsonConvert.DeserializeObject(e.Data);

                if (jData.TryGetValue("event", out var EventToken))
                {
                    string uri;
                    string eventName = EventToken.Value<string>();
                    string extra = "";
                    switch (eventName)
                    {
                        case "mute_changed":
                            extra = data.mute;
                            SetProperty("Mute", extra);
                            break;
                        case "playback_state_changed":
                            extra = data.new_state;
                            SetProperty("State", extra);
                            this.Invoke((MethodInvoker)delegate
                            {
                                switch (extra)
                                {
                                    case "playing":
                                        PauseButton.Enabled = true;
                                        PlayButton.Enabled = false;
                                        break;
                                    default:
                                        PlayButton.Enabled = true;
                                        PauseButton.Enabled = false;
                                        break;
                                }
                            });
                            break;
                        case "playlist_changed":
                            extra = data.playlist.uri;
                            extra = Uri.UnescapeDataString(extra);
                            // TODO
                            break;
                        case "seeked":
                            int TimePosition = data.time_position;
                            extra = TimeSpan.FromMilliseconds(TimePosition).ToString(@"hh\:mm\:ss");
                            // TODO
                            break;
                        case "stream_title_changed":
                            string title = data.title.ToString();
                            SetProperty("Stream Title", title);
                            extra = title;
                            if (!string.IsNullOrWhiteSpace(title))
                            {
                                this.Invoke((MethodInvoker)delegate
                                {
                                    if (title.Length > 63)
                                        trayIcon.Text = "…" + title.Substring(title.Length - 62, 62);
                                    else
                                        trayIcon.Text = title;
                                });
                            }
                            break;
                        case "track_playback_ended":
                            state.Invoke((MethodInvoker)delegate
                            {
                                state.BeginUpdate();
                                try
                                {
                                    SetProperty("Track URI", "");
                                    SetProperty("Track Title", "");
                                    SetProperty("Track Artist(s)", "");
                                    SetProperty("Track Album", "");
                                }
                                finally
                                {
                                    state.EndUpdate();
                                }
                            });
                            extra = data.tl_track.track.uri;
                            extra = Uri.UnescapeDataString(extra);
                            this.Invoke((MethodInvoker)delegate
                            {
                                this.Text = Application.ProductName;
                                this.Invoke((MethodInvoker)delegate
                                {
                                    trayIcon.Text = this.Text;
                                });
                            });
                            // TODO
                            break;
                        case "track_playback_paused":
                        case "track_playback_resumed":
                        case "track_playback_started":
                            uri = data.tl_track.track.uri;
                            uri = Uri.UnescapeDataString(uri);
                            extra = uri;
                            DescribeTrack(data.tl_track, eventName != "track_playback_paused" && checkShowNotifications.Checked);
                            // TODO
                            break;
                        case "volume_changed":
                            extra = data.volume;
                            SetProperty("Volume", extra);
                            break;
                    }
                    Log(JsonConvert.SerializeObject(data.@event, Formatting.Indented) + " - " + extra, EventLogEntryType.SuccessAudit);
                }
                else if (data.error != null)
                {
                    throw new Exception(JsonConvert.SerializeObject(data.error, Formatting.Indented));
                }
                else
                {
                    Log(JsonConvert.SerializeObject(data, Formatting.Indented), EventLogEntryType.Warning);
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString(), EventLogEntryType.Error);
            }
        }

        private void Client_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            Log(e.Message ?? e.Exception?.ToString() ?? "<unknown error>", EventLogEntryType.Warning);
        }

        private void Client_OnOpen(object sender, EventArgs e)
        {
            Log($"Connected to {Mopidy.Uri}");
            SetProperty("Host", Mopidy.Uri);
            this.Invoke((MethodInvoker)delegate
            {
                textURL.Enabled = false;
                buttonURL.Text = "Disconnect";
                ButtonPanel.Enabled = true;
                trayIcon.Icon = Resources.mopidy_icon;
            });
        }

        private void Client_OnClose(object sender, WebSocketSharp.CloseEventArgs e)
        {
            Log($"Disconnected from {Mopidy.Uri} ({e.Reason}).");
            this.Invoke((MethodInvoker)delegate
            {
                textURL.Enabled = true;
                buttonURL.Text = "Connect";
                ButtonPanel.Enabled = false;
                trayIcon.Icon = Resources.mopidy_icon_gray;
            });
        }

        private void Client_OnTrackStarted(object sender, TlTrackEventArgs e)
        {
            DescribeTrack(e.Track, true);
            UpdatePosition(e.Track.Track.Length, DateTime.UtcNow, true);
        }

        private void Client_OnTrackEnded(object sender, TlTrackTimeEventArgs e)
        {
            DescribeTrack(e.Track, false);
            UpdatePosition(e.Track.Track.Length, e.TimePosition, false);
        }

        private void Client_OnTrackPaused(object sender, TlTrackTimeEventArgs e)
        {
            DescribeTrack(e.Track, true);
            UpdatePosition(e.Track.Track.Length, e.TimePosition, false);
        }

        private void Client_OnTrackResumed(object sender, TlTrackTimeEventArgs e)
        {
            DescribeTrack(e.Track, true);
            UpdatePosition(e.Track.Track.Length, e.TimePosition, true);
        }


        private int msgID = new Random(DateTime.Now.Millisecond).Next(10000);

        private void buttonCommand_Click(object sender, EventArgs e)
        {
            string Command = comboCommand.Text;
            if (!comboCommand.Items.Contains(Command))
                comboCommand.Items.Insert(0, Command);
            var Match = Regex.Match(Command, @"([a-z_.]+)\s*(?:\((.*)\))?", RegexOptions.IgnoreCase);
            if (Match.Success)
            {
                dynamic Data;
                if (Match.Groups[2].Success)
                {
                    dynamic parameters = JsonConvert.DeserializeObject("[" + Match.Groups[2].Value + "]");
                    Data = new { jsonrpc = "2.0", id = ++msgID, method = Match.Groups[1].Value, @params = parameters };
                }
                else
                {
                    Data = new { jsonrpc = "2.0", id = ++msgID, method = Match.Groups[1].Value };
                }
                Command = JsonConvert.SerializeObject(Data);
            }
            Mopidy.WebSocket.Send(Command);
            Log(Command, EventLogEntryType.Information);
        }

        private void SetProperty(string key, string value, bool bold = false)
        {
            state.Invoke((MethodInvoker)delegate
            {
                var Items = state.Items.Find(key, false);
                if (string.IsNullOrWhiteSpace(value))
                {
                    foreach (var Item in Items)
                        Item.Remove();
                }
                else
                {
                    if (Items.Length == 0)
                    {
                        var Subitem = state.Items.Insert(0, key, key, -1).SubItems.Add(value);
                        if (bold)
                            Subitem.Font = new Font(Subitem.Font, FontStyle.Bold);
                    }
                    else
                    {
                        var Item = Items[0];
                        var Subitem = Item.SubItems[Item.SubItems.Count - 1];
                        Subitem.Text = value;
                        if (Subitem.Font.Bold != bold)
                        {
                            if (bold)
                                Subitem.Font = new Font(Subitem.Font, FontStyle.Bold);
                            else
                                Subitem.Font = new Font(Subitem.Font, 0);
                        }
                    }
                }
                foreach (ColumnHeader column in state.Columns)
                    column.Width = -2;
            });
        }

        private void Log(string message, EventLogEntryType type = EventLogEntryType.Information)
        {
            int ImageIndex = (int)Math.Round(Math.Log((int)type) / Math.Log(2));
            state.Invoke((MethodInvoker)delegate
            {
                state.Items.Add(null, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), ImageIndex).SubItems.Add(message);
                foreach (ColumnHeader column in state.Columns)
                    column.Width = -2;
            });
        }

        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            // TODO: implement this event in TrayIcon
            if (this.Visible)
            {
                this.WindowState = FormWindowState.Minimized;
                this.Visible = false;
            }
            else
            {
                this.Visible = true;
                if (this.WindowState == FormWindowState.Minimized)
                    this.Restore();
            }
        }

        private void state_DoubleClick(object sender, EventArgs e)
        {
            if (state.FocusedItem != null)
            {
                MessageBox.Show(state.FocusedItem.SubItems[state.FocusedItem.SubItems.Count - 1].Text, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void NextButton_Click(object sender, EventArgs e)
        {
            Mopidy.Execute("core.playback.next");
        }

        private void PrevButton_Click(object sender, EventArgs e)
        {
            Mopidy.Execute("core.playback.previous");
        }

        private void PauseButton_Click(object sender, EventArgs e)
        {
            Mopidy.Execute("core.playback.pause");
        }

        private void PlayButton_Click(object sender, EventArgs e)
        {
            Mopidy.Execute("core.playback.play");
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.MediaNextTrack:
                    NextButton.PerformClick();
                    e.SuppressKeyPress = true;
                    break;
                case Keys.MediaPlayPause:
                    if (PlayButton.Enabled)
                        PlayButton.PerformClick();
                    else
                        PauseButton.PerformClick();
                    e.SuppressKeyPress = true;
                    break;
                case Keys.MediaPreviousTrack:
                    PrevButton.PerformClick();
                    e.SuppressKeyPress = true;
                    break;
            }
        }

        private class TrackInfo
        {
            public string Title;
            public string Artists;
            public string Album;

            private static string CleanUri(dynamic model)
            {
                if (model.uri == null)
                    return null;
                string uri = model.uri;
                uri = uri.Substring(uri.IndexOf(':') + 1);
                uri = Uri.UnescapeDataString(uri);
                return uri;
            }
            private static string CleanUri(Track model)
            {
                if (model.Uri == null)
                    return null;
                string uri = model.Uri;
                uri = uri.Substring(uri.IndexOf(':') + 1);
                uri = Uri.UnescapeDataString(uri);
                return uri;
            }

            public static TrackInfo FromTrack(Models.Track track, bool onlyOriginal)
            {
                var trackInfo = new TrackInfo();
                // Track title = name
                if (track.Name != null)
                {
                    trackInfo.Title = track.Name;
                }
                else if (!onlyOriginal)
                {
                    trackInfo.Title = CleanUri(track);
                }

                // Track album
                if (track.Album != null && track.Album.Name != null)
                {
                    trackInfo.Album = track.Album.Name;
                }

                // Track artist(s) / composer(s)
                var Artists = new List<string>();
                if (track.Artists != null)
                {
                    foreach (var Artist in track.Artists)
                        Artists.Add(Artist.Name);
                }
                else if (track.Album != null && track.Album.Artists != null)
                {
                    foreach (var Artist in track.Album.Artists)
                        Artists.Add(Artist.Name);
                }
                else if (track.Composers != null)
                {
                    foreach (var composer in track.Composers)
                        Artists.Add(composer.Name);
                }
                if (Artists.Count > 0)
                {
                    trackInfo.Artists = string.Join(", ", Artists);
                }
                else if (!onlyOriginal)
                {
                    trackInfo.Artists = CleanUri(track);
                }

                // Check if track and artists are both track.uri; if so, try to split up the uri-as-filename
                if (!onlyOriginal && (trackInfo.Title == trackInfo.Artists))
                {
                    var pos = trackInfo.Title.LastIndexOf("/");
                    if (pos > 0)
                    {
                        trackInfo.Album = trackInfo.Title.Substring(0, pos);
                        trackInfo.Title = trackInfo.Title.Substring(pos + 1);
                    }
                }

                return trackInfo;
            }
            public static TrackInfo FromTrack(dynamic track, bool onlyOriginal)
            {
                TrackInfo trackInfo = null;
                if (track != null && track.__model__ == "Track")
                {
                    // TODO:
                }
                return trackInfo;
            }

            public static TrackInfo FromTracklistTrack(Models.TlTrack tlTrack, bool onlyOriginal)
            {
                return FromTrack(tlTrack.Track, onlyOriginal);
            }
            public static TrackInfo FromTracklistTrack(dynamic tlTrack, bool onlyOriginal)
            {
                TrackInfo trackInfo = null;
                if (tlTrack != null && tlTrack.__model__ != null)
                {
                    switch (tlTrack.__model__.ToString())
                    {
                        case "TlTrack":
                            trackInfo = FromTrack(tlTrack.track, onlyOriginal);
                            break;
                        case "Track":
                            trackInfo = FromTrack(tlTrack, onlyOriginal);
                            break;
                    }
                }
                return trackInfo;
            }
        } // class TrackInfo

        private void ReconnectMopidy(string uri)
        {
            if (Mopidy != null)
            {
                Mopidy.Dispose();
                Mopidy = null;
            }
            Mopidy = new MopidyClient(uri);
            Mopidy.OnConnect += Client_OnOpen;
            Mopidy.OnDisconnect += Client_OnClose;
            Mopidy.OnError += Client_OnError;
            Mopidy.OnMessage += Client_OnMessage;
            Mopidy.OnTrackStarted += Client_OnTrackStarted;
            Mopidy.OnTrackPaused += Client_OnTrackPaused;
            Mopidy.OnTrackResumed += Client_OnTrackResumed;
            Mopidy.OnTrackEnded += Client_OnTrackEnded;
            Mopidy.Connect();
        }

        private void buttonURL_Click(object sender, EventArgs e)
        {
            if (Mopidy.IsConnected)
            {
                Mopidy.Disconnect();
            }
            else
            {
                ReconnectMopidy(textURL.Text);
            }
        }

        private void timerPosition_Tick(object sender, EventArgs e)
        {
            var Started = (DateTime)timerPosition.Tag;
            var Position = (int)Math.Round((DateTime.UtcNow - Started).TotalMilliseconds);
            if (Position <= trackPosition.Maximum)
                trackPosition.Value = Position;
            else
                trackPosition.Value = trackPosition.Maximum;
            UpdatePositionText();
        }

        private void UpdatePositionText()
        {
            var Position = trackPosition.Value;
            var PositionText = TimeSpan.FromMilliseconds(Position).ToString(@"h\:mm\:ss");
            if (PositionText.StartsWith("0:"))
                PositionText = PositionText.Substring(3);

            var Length = trackPosition.Maximum;
            var LengthText = TimeSpan.FromMilliseconds(Length).ToString(@"h\:mm\:ss");
            if (LengthText.StartsWith("0:"))
                LengthText = LengthText.Substring(3);

            textPosition.Text = $"{PositionText} / {LengthText}";
        }

        private void AdjustTickFrequency()
        {
            if (trackPosition.Maximum > 4 * 60 * 60 * 1000)
                trackPosition.TickFrequency = 30 * 60 * 1000; // each half hour
            else if (trackPosition.Maximum > 40 * 60 * 1000)
                trackPosition.TickFrequency = 5 * 60 * 1000; // per 5 minutes
            else if (trackPosition.Maximum > 8 * 60 * 1000)
                trackPosition.TickFrequency = 60 * 1000; // per 1 minute
            else
                trackPosition.TickFrequency = 5 * 1000;
        }

        private void UpdatePosition(int? maximum, DateTime start, bool tracking)
        {
            this.Invoke((MethodInvoker)delegate
            {
                timerPosition.Enabled = false;
                timerPosition.Tag = start;
                trackPosition.Maximum = maximum ?? 0;
                trackPosition.Value = 0;
                timerPosition.Enabled = tracking;
                AdjustTickFrequency();
                UpdatePositionText();
            });
        }
        private void UpdatePosition(int? maximum, int position, bool tracking)
        {
            this.Invoke((MethodInvoker)delegate
            {
                timerPosition.Enabled = false;
                timerPosition.Tag = DateTime.UtcNow.AddMilliseconds(-position);
                trackPosition.Maximum = maximum ?? 0;
                trackPosition.Value = position;
                timerPosition.Enabled = tracking;
                AdjustTickFrequency();
                UpdatePositionText();
            });
        }

        private async void trackPosition_Scroll(object sender, EventArgs e)
        {
            await Mopidy.SeekAsync(trackPosition.Value);
        }

    }
}


namespace System.Windows.Forms
{

    public static class Extensions
    {
        [DllImport("user32.dll")]
        private static extern int ShowWindow(IntPtr hWnd, uint Msg);

        private const uint SW_RESTORE = 0x09;

        public static void Restore(this Form form)
        {
            if (form.WindowState == FormWindowState.Minimized)
            {
                ShowWindow(form.Handle, SW_RESTORE);
            }
        }

    }

}