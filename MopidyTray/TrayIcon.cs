using System.Drawing;
using System.Runtime.InteropServices;

namespace System.Windows.Forms
{
    class TrayIcon : IDisposable
    {
        private NOTIFYICONDATA _nid;
        private Form _parent;
        private Icon _icon;

        public TrayIcon(Form parent, string text, Icon icon = null, bool visible = true)
        {
            _parent = parent;
            _icon = icon;
            _nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                hWnd = parent.Handle,
                uID = 0,
                uFlags = NIF.ICON | NIF.TIP | NIF.SHOWTIP,
                hIcon = (_icon ?? parent.Icon).Handle,
                szTip = text ?? parent.Text,
                dwState = visible ? 0 : NIS.HIDDEN,
                dwStateMask = NIS.HIDDEN,
                uTimeoutOrVersion = NOTIFYICON_VERSION_4
            };
            Shell_NotifyIcon(NIM.SETVERSION, ref _nid);
            Shell_NotifyIcon(NIM.ADD, ref _nid);
        }

        public Icon Icon
        {
            get
            {
                return _icon;
            }
            set
            {
                _icon = value;
                _nid.hIcon = (_icon ?? _parent.Icon).Handle;
                Shell_NotifyIcon(NIM.MODIFY, ref _nid);
            }
        }

        public string Text
        {
            get
            {
                return _nid.szTip;
            }
            set
            {
                _nid.szTip = value;
                Shell_NotifyIcon(NIM.MODIFY, ref _nid);
            }
        }

        public bool Visible
        {
            get
            {
                return (_nid.dwState & NIS.HIDDEN) == 0;
            }
            set
            {
                if (value)
                    _nid.dwState &= ~NIS.HIDDEN;
                else
                    _nid.dwState |= NIS.HIDDEN;
                Shell_NotifyIcon(NIM.MODIFY, ref _nid);
            }
        }

        public bool ShowNotification(string title, string text, MessageBoxIcon icon, bool silent = false)
        {
            var Flags = NIIF.NONE;
            switch (icon)
            {
                case MessageBoxIcon.None:
                    Flags = NIIF.NONE;
                    break;
                case MessageBoxIcon.Information:
                    Flags = NIIF.INFO;
                    break;
                case MessageBoxIcon.Warning:
                    Flags = NIIF.WARNING;
                    break;
                case MessageBoxIcon.Error:
                    Flags = NIIF.ERROR;
                    break;
            }
            if (silent)
                Flags |= NIIF.NOSOUND;
            _nid.hBalloonIcon = IntPtr.Zero;
            return ShowNotification(title, text, Flags);
        }
        public bool ShowNotification(string title, string text, Icon icon, bool silent = false)
        {
            var Flags = NIIF.USER;
            if (silent)
                Flags |= NIIF.NOSOUND;
            _nid.hBalloonIcon = (icon ?? _parent.Icon).Handle;
            return ShowNotification(title, text, Flags);
        }
        public bool ShowNotification(string title, string text, NIIF flags = NIIF.NONE)
        {
            _nid.uFlags |= NIF.INFO;
            _nid.szInfoTitle = title;
            _nid.szInfo = text;
            _nid.dwInfoFlags = flags;
            bool Result = Shell_NotifyIcon(NIM.MODIFY, ref _nid);
            _nid.uFlags &= ~NIF.INFO;
            return Result;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // free unmanaged resources (unmanaged objects) and override a finalizer below.
                Shell_NotifyIcon(NIM.DELETE, ref _nid);
                // set large fields to null.

                disposedValue = true;
            }
        }

        ~TrayIcon()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool Shell_NotifyIcon(NIM message, ref NOTIFYICONDATA data);

        private enum NIM : uint
        {
            ADD = 0x00,
            MODIFY = 0x01,
            DELETE = 0x02,
            SETFOCUS = 0x03,
            SETVERSION = 0x04,
        }

        [Flags]
        private enum NIF : uint
        {
            MESSAGE = 0x01,
            ICON = 0x02,
            TIP = 0x04,
            STATE = 0x08,
            INFO = 0x10,
            GUID = 0x20,
            REALTIME = 0x40,
            SHOWTIP = 0x80,
        }

        [Flags]
        private enum NIS : uint
        {
            HIDDEN = 1,
            SHAREDICON = 2,
        }

        private const int NOTIFYICON_VERSION_4 = 4;

        [Flags]
        public enum NIIF : uint
        {
            NONE = 0x00,
            INFO = 0x01,
            WARNING = 0x02,
            ERROR = 0x03,
            USER = 0x04,

            NOSOUND = 0x10,
            LARGE_ICON = 0x20,
            RESPECT_QUIET_TIME = 0x80,
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NOTIFYICONDATA
        {
            public int cbSize /*= Marshal.SizeOf(typeof(NOTIFYICONDATA))*/;
            public IntPtr hWnd;
            public uint uID;
            public NIF uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x80)]
            public string szTip;
            public NIS dwState;
            public NIS dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x100)]
            public string szInfo;
            public uint uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x40)]
            public string szInfoTitle;
            public NIIF dwInfoFlags;
            public Guid guidItem;
            public IntPtr hBalloonIcon;
        }
    }
}
