﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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
            notifyIcon.Icon = Properties.Resources.mopidy_icon_gray;
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
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (!EventClient.IsAlive)
                EventClient.Close();
            notifyIcon.Visible = false;
        }

        private void Client_OnMessage(object sender, WebSocketSharp.MessageEventArgs e)
        {
            try
            {
                dynamic data = JsonConvert.DeserializeObject(e.Data);

                if (data.@event != null)
                {
                    string uri;
                    Log(JsonConvert.SerializeObject(data.@event, Formatting.Indented), EventLogEntryType.SuccessAudit);
                    string eventName = data.@event;
                    switch (eventName)
                    {
                        case "mute_changed":
                            SetProperty("mute", data.mute.ToString());
                            break;
                        case "playback_state_changed":
                            SetProperty("state", data.new_state.ToString());
                            break;
                        case "playlist_changed":
                            // TODO
                            break;
                        case "seeked":
                            // TODO
                            break;
                        case "stream_title_changed":
                            string title = data.title.ToString();
                            SetProperty("stream_title", title);
                            if (!string.IsNullOrWhiteSpace(title))
                            {
                                if (title.Length > 63)
                                    notifyIcon.Text = title.Substring(title.Length - 63, 63);
                                else
                                    notifyIcon.Text = title;
                            }
                            break;
                        case "track_playback_ended":
                            SetProperty("track", "");
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
                            SetProperty("volume", data.volume.ToString());
                            break;
                    }
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
            notifyIcon.Icon = Properties.Resources.mopidy_icon;
        }

        private void Client_OnClose(object sender, WebSocketSharp.CloseEventArgs e)
        {
            notifyIcon.Icon = Properties.Resources.mopidy_icon_gray;
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
                state.Items.Add(null, DateTime.Now.ToString("HH:mm:ss.fff"), imageIndex).SubItems.Add(message);
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