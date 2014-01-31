// Some interop code taken from Mike Marshall's AnyForm
namespace NetInject.NotifyIcon.Interop {
    using System;
    using System.Drawing;
    using System.Runtime.InteropServices;
    /// <summary>
    ///     Resolves the current tray position.
    /// </summary>
    public static class TrayInfo {
        /// <summary>
        ///     Gets the position of the system tray.
        /// </summary>
        /// <returns>Tray coordinates.</returns>
        public static Point GetTrayLocation() {
            var info = new AppBarInfo();
            info.GetSystemTaskBarPosition();
            Rectangle rcWorkArea = info.WorkArea;
            int x = 0, y = 0;
            if (info.Edge == AppBarInfo.ScreenEdge.Left) {
                x = rcWorkArea.Left + 2;
                y = rcWorkArea.Bottom;
            } else if (info.Edge == AppBarInfo.ScreenEdge.Bottom) {
                x = rcWorkArea.Right;
                y = rcWorkArea.Bottom;
            } else if (info.Edge == AppBarInfo.ScreenEdge.Top) {
                x = rcWorkArea.Right;
                y = rcWorkArea.Top;
            } else if (info.Edge == AppBarInfo.ScreenEdge.Right) {
                x = rcWorkArea.Right;
                y = rcWorkArea.Bottom;
            }
            return new Point {X = x, Y = y};
        }
    }
    internal class AppBarInfo {
        public enum ScreenEdge {
            Left = AbeLeft,
            Top = AbeTop,
            Right = AbeRight,
            Bottom = AbeBottom
        }
        private const int AbeBottom = 3;
        private const int AbeLeft = 0;
        private const int AbeRight = 2;
        private const int AbeTop = 1;
        private const int AbmGettaskbarpos = 0x00000005;

        // SystemParametersInfo constants
        private const UInt32 SpiGetworkarea = 0x0030;
        private Appbardata _mData;
        public ScreenEdge Edge {
            get { return (ScreenEdge)_mData.uEdge; }
        }
        public Rectangle WorkArea {
            get {
                var rc = new Rect();
                IntPtr rawRect = Marshal.AllocHGlobal(Marshal.SizeOf(rc));
                int bResult = SystemParametersInfo(SpiGetworkarea, 0, rawRect, 0);
                rc = (Rect)Marshal.PtrToStructure(rawRect, rc.GetType());
                if (bResult == 1) {
                    Marshal.FreeHGlobal(rawRect);
                    return new Rectangle(rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top);
                }
                return new Rectangle(0, 0, 0, 0);
            }
        }
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(String lpClassName, String lpWindowName);
        [DllImport("shell32.dll")]
        private static extern UInt32 SHAppBarMessage(UInt32 dwMessage, ref Appbardata data);
        [DllImport("user32.dll")]
        private static extern Int32 SystemParametersInfo(UInt32 uiAction, UInt32 uiParam,
            IntPtr pvParam, UInt32 fWinIni);
        private void GetPosition(string strClassName, string strWindowName) {
            _mData = new Appbardata();
            _mData.cbSize = (UInt32)Marshal.SizeOf(_mData.GetType());
            IntPtr hWnd = FindWindow(strClassName, strWindowName);
            if (hWnd != IntPtr.Zero) {
                UInt32 uResult = SHAppBarMessage(AbmGettaskbarpos, ref _mData);
                if (uResult != 1)
                    throw new Exception("Failed to communicate with the given AppBar");
            } else
                throw new Exception("Failed to find an AppBar that matched the given criteria");
        }
        public void GetSystemTaskBarPosition() {
            GetPosition("Shell_TrayWnd", null);
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct Appbardata {
            public UInt32 cbSize;
            private readonly IntPtr hWnd;
            private readonly UInt32 uCallbackMessage;
            public readonly UInt32 uEdge;
            private readonly Rect rc;
            private readonly Int32 lParam;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct Rect {
            public readonly Int32 left;
            public readonly Int32 top;
            public readonly Int32 right;
            public readonly Int32 bottom;
        }
    }
}