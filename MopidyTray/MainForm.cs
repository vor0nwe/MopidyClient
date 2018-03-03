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
            //notifyIcon.Text = this.Text;
            //notifyIcon.Icon = Resources.mopidy_icon_gray;
            //notifyIcon.Visible = true;
            this.ShowTrayIcon(this.Text, Resources.mopidy_icon_gray);

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
            //notifyIcon.Visible = false;
            this.HideTrayIcon();
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
                                        //notifyIcon.Text = "…" + title.Substring(title.Length - 62, 62);
                                        this.ModifyTrayIcon("…" + title.Substring(title.Length - 62, 62));
                                    else
                                        //notifyIcon.Text = title;
                                        this.ModifyTrayIcon(title);
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
                                    //notifyIcon.Text = this.Text;
                                    this.ModifyTrayIcon(this.Text);
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
                                this.Invoke((MethodInvoker)delegate
                                {
                                    //notifyIcon.BalloonTipIcon = ToolTipIcon.None;
                                    //notifyIcon.BalloonTipText = uri;
                                    //notifyIcon.ShowBalloonTip(3000);
                                    this.ShowTrayNotification(Application.ProductName, uri, MessageBoxIcon.None, true);
                                });
                            }
                            this.Invoke((MethodInvoker)delegate
                            {
                                this.Text = uri + " - " + Application.ProductName;
                                uri = Path.ChangeExtension(uri, "").TrimEnd('.');
                                if (uri.Length > 63)
                                    //notifyIcon.Text = "…" + uri.Substring(uri.Length - 62, 62);
                                    this.ModifyTrayIcon("…" + uri.Substring(uri.Length - 62, 62));
                                else
                                    //notifyIcon.Text = uri;
                                    this.ModifyTrayIcon(uri);
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
                //notifyIcon.Icon = Resources.mopidy_icon;
                this.ModifyTrayIcon(this.Text, Resources.mopidy_icon);
            });
        }

        private void Client_OnClose(object sender, WebSocketSharp.CloseEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                //notifyIcon.Icon = Resources.mopidy_icon_gray;
                this.ModifyTrayIcon(this.Text, Resources.mopidy_icon_gray);
            });
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
            Log(Command, EventLogEntryType.Information);
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


        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool Shell_NotifyIcon(uint message, ref NOTIFYICONDATA data);

        private const uint NIM_ADD = 0x00;
        private const uint NIM_MODIFY = 0x01;
        private const uint NIM_DELETE = 0x02;
        private const uint NIM_SETFOCUS = 0x03;
        private const uint NIM_SETVERSION = 0x04;

        private const int NIF_MESSAGE = 0x01;
        private const int NIF_ICON = 0x02;
        private const int NIF_TIP = 0x04;
        private const int NIF_STATE = 0x08;
        private const int NIF_INFO = 0x10;
        private const int NIF_GUID = 0x20;
        private const int NIF_REALTIME = 0x40;
        private const int NIF_SHOWTIP = 0x80;

        private const int NOTIFYICON_VERSION_4 = 4;

        private const int NIIF_NONE = 0x00;
        private const int NIIF_INFO = 0x01;
        private const int NIIF_WARNING = 0x02;
        private const int NIIF_ERROR = 0x03;
        private const int NIIF_USER = 0x04;

        private const int NIIF_NOSOUND = 0x10;
        private const int NIIF_LARGE_ICON = 0x20;
        private const int NIIF_RESPECT_QUIET_TIME = 0x80;

        private static uint IconID;

        public static void ShowTrayIcon(this Form form, string tip, Icon icon = null)
        {
            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                hWnd = form.Handle,
                uID = 0,
                uFlags = NIF_ICON | NIF_TIP | NIF_SHOWTIP,
                hIcon = (icon == null ? form.Icon.Handle : icon.Handle),
                szTip = tip,
                uTimeoutOrVersion = NOTIFYICON_VERSION_4
            };
            Shell_NotifyIcon(NIM_SETVERSION, ref nid);
            Shell_NotifyIcon(NIM_ADD, ref nid);
            IconID = nid.uID;
        }

        public static void ModifyTrayIcon(this Form form, string tip, Icon icon = null)
        {
            uint Flags = NIF_TIP | NIF_SHOWTIP;
            if (icon != null)
                Flags |= NIF_ICON;
            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                hWnd = form.Handle,
                uID = IconID,
                uFlags = Flags,
                hIcon = (icon == null ? IntPtr.Zero : icon.Handle),
                szTip = tip,
                uTimeoutOrVersion = NOTIFYICON_VERSION_4
            };
            Shell_NotifyIcon(NIM_MODIFY, ref nid);
        }

        public static void HideTrayIcon(this Form form)
        {
            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                hWnd = form.Handle,
                uID = IconID,
                uFlags = NIF_ICON | NIF_SHOWTIP,
                hIcon = form.Icon.Handle,
                szTip = null,
                dwState = 0,
                dwStateMask = 0,
                szInfo = null,
                szInfoTitle = null
            };
            Shell_NotifyIcon(NIM_DELETE, ref nid);
        }

        public static void ShowTrayNotification(this Form form, string title, string text, MessageBoxIcon icon = MessageBoxIcon.None, bool silent = false)
        {
            uint InfoFlags = NIIF_NONE;
            switch(icon)
            {
                case MessageBoxIcon.None:
                    InfoFlags = NIIF_NONE;
                    break;
                case MessageBoxIcon.Information:
                    InfoFlags = NIIF_INFO;
                    break;
                case MessageBoxIcon.Warning:
                    InfoFlags = NIIF_WARNING;
                    break;
                case MessageBoxIcon.Error:
                    InfoFlags = NIIF_ERROR;
                    break;
            }
            if (silent)
                InfoFlags |= NIIF_NOSOUND;

            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                hWnd = form.Handle,
                uID = IconID,
                uFlags = NIF_ICON | NIF_INFO,
                hIcon = form.Icon.Handle,
                dwState = 0,
                dwStateMask = 0,
                szInfo = text,
                szInfoTitle = title,
                dwInfoFlags = InfoFlags
            };
            Shell_NotifyIcon(NIM_MODIFY, ref nid);
        }
        public static void ShowTrayNotification(this Form form, string title, string text, Icon icon, bool silent = false)
        {
            uint InfoFlags = NIIF_USER;
            if (silent)
                InfoFlags |= NIIF_NOSOUND;
            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                hWnd = form.Handle,
                uID = IconID,
                uFlags = NIF_ICON | NIF_INFO,
                hIcon = form.Icon.Handle,
                dwState = 0,
                dwStateMask = 0,
                szInfo = text,
                szInfoTitle = title,
                dwInfoFlags = InfoFlags,
                hBalloonIcon = icon.Handle
            };
            Shell_NotifyIcon(NIM_MODIFY, ref nid);
        }



    }

    //[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    //class NOTIFYICONDATA
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct NOTIFYICONDATA
    {
        public int cbSize /*= Marshal.SizeOf(typeof(NOTIFYICONDATA))*/;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x80)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x100)]
        public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x40)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

}