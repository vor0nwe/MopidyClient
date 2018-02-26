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

        private void MainForm_Load(object sender, EventArgs e)
        {
            notifyIcon.Text = this.Text;
            notifyIcon.Icon = Resources.mopidy_icon_gray;
            notifyIcon.Visible = true;

            imageList.Images.Add(SystemIcons.Error);
            imageList.Images.Add(SystemIcons.Warning);
            imageList.Images.Add(SystemIcons.Information);
            imageList.Images.Add(SystemIcons.Application);
            imageList.Images.Add(SystemIcons.Question);

            EventClient = new WebSocketSharp.WebSocket("ws://192.168.178.13:6680/mopidy/ws");
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
            notifyIcon.Visible = false;
            var Commands = new List<string>();
            foreach(string Item in comboCommand.Items)
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
                                if (title.Length > 63)
                                    notifyIcon.Text = "…" + title.Substring(title.Length - 62, 62);
                                else
                                    notifyIcon.Text = title;
                            }
                            break;
                        case "track_playback_ended":
                            SetProperty("track", "");
                            extra = data.tl_track.track.uri;
                            extra = Uri.UnescapeDataString(extra);
                            this.Invoke((MethodInvoker)delegate
                            {
                                this.Text = Application.ProductName;
                                notifyIcon.Text = this.Text;
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
                            if (eventName != "track_playback_paused")
                            {
                                notifyIcon.BalloonTipIcon = ToolTipIcon.None;
                                notifyIcon.BalloonTipText = uri;
                                if (checkShowNotifications.Checked)
                                    notifyIcon.ShowBalloonTip(3000);
                            }
                            this.Invoke((MethodInvoker)delegate
                            {
                                this.Text = uri + " - " + Application.ProductName;
                            });
                            uri = Path.ChangeExtension(uri, "").TrimEnd('.');
                            if (uri.Length > 63)
                                notifyIcon.Text = "…" + uri.Substring(uri.Length - 62, 62);
                            else
                                notifyIcon.Text = uri;
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
            notifyIcon.Icon = Resources.mopidy_icon;
        }

        private void Client_OnClose(object sender, WebSocketSharp.CloseEventArgs e)
        {
            notifyIcon.Icon = Resources.mopidy_icon_gray;
        }

        private int msgID = new Random(DateTime.Now.Millisecond).Next(10000);

        private void buttonCommand_Click(object sender, EventArgs e)
        {
            var Command = comboCommand.Text;
            if (!comboCommand.Items.Contains(Command))
                comboCommand.Items.Insert(0, Command);
            var match = Regex.Match(Command, @"([a-z_.]+)\s*(?:\((.*)\))?", RegexOptions.IgnoreCase);
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
                Command = JsonConvert.SerializeObject(data);
            }
            EventClient.Send(Command);
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
            });
        }

        private void Log(string message, EventLogEntryType type = EventLogEntryType.Information)
        {
            int imageIndex = (int)Math.Round(Math.Log((int)type) / Math.Log(2));
            state.Invoke((MethodInvoker)delegate
            {
                state.Items.Add(null, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), imageIndex).SubItems.Add(message);
                foreach (ColumnHeader column in state.Columns)
                    column.Width = -2;
            });
        }

        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
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