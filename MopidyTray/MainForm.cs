using MopidyTray.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MopidyTray
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        public WebSocketSharp.WebSocket EventClient;
        private TrayIcon trayIcon;

        private void MainForm_Load(object sender, EventArgs e)
        {
            trayIcon = new TrayIcon(this, this.Text, Resources.mopidy_icon_gray);

            imageList.Images.Add(SystemIcons.Error);
            imageList.Images.Add(SystemIcons.Warning);
            imageList.Images.Add(SystemIcons.Information);
            imageList.Images.Add(SystemIcons.Application);
            imageList.Images.Add(SystemIcons.Question);

            if (!Settings.Default.URIConfirmed)
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

            EventClient = new WebSocketSharp.WebSocket(Settings.Default.HostUri);
            EventClient.OnOpen += Client_OnOpen;
            EventClient.OnClose += Client_OnClose;
            EventClient.OnError += Client_OnError;
            EventClient.OnMessage += Client_OnMessage;
            EventClient.ConnectAsync();

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
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (!EventClient.IsAlive)
                EventClient.Close();
            
            trayIcon.Dispose();
            trayIcon = null;

            var Commands = new List<string>();
            foreach (string Item in comboCommand.Items)
            {
                Commands.Add(Item);
            }
            Commands.Sort();
            Settings.Default["Commands"] = string.Join(Environment.NewLine, Commands);
            Settings.Default.Save();
        }

        private void Client_OnMessage(object sender, WebSocketSharp.MessageEventArgs e)
        {
            try
            {
                dynamic data = JsonConvert.DeserializeObject(e.Data);

                if (data.@event != null)
                {
                    string uri;
                    string eventName = data.@event;
                    string extra = "";
                    switch (eventName)
                    {
                        case "mute_changed":
                            extra = data.mute;
                            SetProperty("mute", extra);
                            break;
                        case "playback_state_changed":
                            extra = data.new_state;
                            SetProperty("state", extra);
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
                            SetProperty("stream_title", title);
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
                            SetProperty("track", "");
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
                            SetProperty("track", uri);
                            extra = uri;
                            uri = uri.Substring(uri.IndexOf(':') + 1);
                            if (eventName != "track_playback_paused" && checkShowNotifications.Checked)
                            {
                                TrackInfo track = TrackInfo.FromTracklistTrack(data.tl_track);
                                if (track != null)
                                {
                                    string Description = track.Artists;
                                    if (!string.IsNullOrWhiteSpace(track.Album))
                                        Description += Environment.NewLine + "(" + track.Album + ")";
                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        trayIcon.ShowNotification(track.Title, Description.Trim(), MessageBoxIcon.None, true);
                                    });
                                }
                            }
                            this.Invoke((MethodInvoker)delegate
                            {
                                this.Text = uri + " - " + Application.ProductName;
                                uri = Path.ChangeExtension(uri, "").TrimEnd('.');
                                if (uri.Length > 63)
                                    trayIcon.Text = "…" + uri.Substring(uri.Length - 62, 62);
                                else
                                    trayIcon.Text = uri;
                            });
                            // TODO
                            break;
                        case "volume_changed":
                            extra = data.volume;
                            SetProperty("volume", extra);
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
            Log(e.Exception.ToString(), EventLogEntryType.Warning);
        }

        private void Client_OnOpen(object sender, EventArgs e)
        {
            SetProperty("Host", EventClient.Url.ToString());
            this.Invoke((MethodInvoker)delegate
            {
                ButtonPanel.Enabled = true;
                trayIcon.Icon = Resources.mopidy_icon;
            });
        }

        private void Client_OnClose(object sender, WebSocketSharp.CloseEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                ButtonPanel.Enabled = false;
                trayIcon.Icon = Resources.mopidy_icon_gray;
            });
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
            EventClient.Send(Command);
            Log(Command, EventLogEntryType.Information);
        }

        private int SendCommand(string command, params string[] parameters)
        {
            var ID = ++msgID;
            dynamic Data = new { jsonrpc = "2.0", id = ID, method = command, @params = parameters };
            string Command = JsonConvert.SerializeObject(Data);
            EventClient.Send(Command);
            Log(Command, EventLogEntryType.Information);
            return ID;
        }

        private void SetProperty(string key, string value)
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
                        state.Items.Insert(0, key, key, -1).SubItems.Add(value);
                    }
                    else
                    {
                        var Item = Items[0];
                        Item.SubItems[Item.SubItems.Count - 1].Text = value;
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
            SendCommand("core.playback.next");
        }

        private void PrevButton_Click(object sender, EventArgs e)
        {
            SendCommand("core.playback.previous");
        }

        private void PauseButton_Click(object sender, EventArgs e)
        {
            SendCommand("core.playback.pause");
        }

        private void PlayButton_Click(object sender, EventArgs e)
        {
            SendCommand("core.playback.play");
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

            public static TrackInfo FromTrack(dynamic track)
            {
                TrackInfo trackInfo = null;
                if (track != null && track.__model__ == "Track")
                {
                    trackInfo = new TrackInfo();
                    // Track title = name
                    if (track.name != null)
                    {
                        trackInfo.Title = track.name;
                    }
                    else
                    {
                        trackInfo.Title = Uri.UnescapeDataString(track.uri.ToString());
                    }

                    // Track album
                    if (track.album != null && track.album.name != null)
                    {
                        trackInfo.Album = track.album.name;
                    }

                    // Track artist(s) / composer(s)
                    var Artists = new List<string>();
                    if (track.artists != null)
                    {
                        foreach (dynamic artist in track.artists)
                        {
                            string name = artist.name;
                            Artists.Add(name);
                        }
                    }
                    else if (track.album != null && track.album.artists != null)
                    {
                        foreach (dynamic artist in track.album.artists)
                        {
                            string name = artist.name;
                            Artists.Add(name);
                        }
                    }
                    else if (track.composers != null)
                    {
                        foreach (dynamic composer in track.composers)
                        {
                            string name = composer.name;
                            Artists.Add(name);
                        }
                    }
                    if (Artists.Count > 0)
                    {
                        trackInfo.Artists = string.Join(", ", Artists);
                    }
                    else
                    {
                        trackInfo.Artists = Uri.UnescapeDataString(track.uri.ToString());
                    }

                    // Check if track and artists are both track.uri; if so, try to split up the uri-as-filename
                    if (trackInfo.Title == trackInfo.Artists)
                    {
                        var pos = trackInfo.Title.LastIndexOf("/");
                        if (pos > 0)
                        {
                            trackInfo.Album = trackInfo.Title.Substring(0, pos);
                            trackInfo.Title = trackInfo.Title.Substring(pos + 1);
                        }
                    }
                }
                return trackInfo;
            }

            public static TrackInfo FromTracklistTrack(dynamic tlTrack)
            {
                TrackInfo trackInfo = null;
                if (tlTrack != null && tlTrack.__model__ != null)
                {
                    switch (tlTrack.__model__.ToString())
                    {
                        case "TlTrack":
                            trackInfo = FromTrack(tlTrack.track);
                            break;
                        case "Track":
                            trackInfo = FromTrack(tlTrack);
                            break;
                    }
                }
                return trackInfo;
            }
        } // class TrackInfo

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