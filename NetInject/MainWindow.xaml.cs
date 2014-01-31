//
//  Author: gReY
//  Contact: mr.greywater+netinject@gmail.com
//  Software: NetInject
//  This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. 
//  If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
namespace NetInject {
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Interop;
    using Annotations;
    using NotifyIcon;
    using ViewModel;
    /// <summary>
    ///     Main window (Injector view)
    /// </summary>
    /// s
    public partial class MainWindow {
        private const Int32 WmSyscommand = 0x112;
        private const Int32 MfSeparator = 0x800;
        private const Int32 MfByposition = 0x400;
        private const Int32 SettingsSysMenuId = 1000;
        private const Int32 AboutSysMenuId = 1001;
        public MainWindow() {
            if (!Util.IsDesignMode) {
                //Minimize to Tray
                StateChanged += (sender, args) => {
                    switch (WindowState) {
                        case WindowState.Minimized:
                            Hide();
                            ShowInTaskbar = false;
                            TrayMenu.Visibility = Visibility.Visible;
                            break;
                        case WindowState.Maximized:
                        case WindowState.Normal:
                            Show();
                            ShowInTaskbar = true;
                            TrayMenu.Visibility = Visibility.Hidden;
                            break;
                    }
                };
                //Additional Window Menu Hook
                Loaded += (sender, args) => {
                    IntPtr systemMenuHandle = GetSystemMenu(new WindowInteropHelper(this).Handle, false);
                    InsertMenu(systemMenuHandle, 5, MfByposition | MfSeparator, 0, string.Empty);
                    InsertMenu(systemMenuHandle, 6, MfByposition, SettingsSysMenuId, "Open Settings");
                    InsertMenu(systemMenuHandle, 7, MfByposition, AboutSysMenuId, "About NetInject");
                    HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
                    if (source != null)
                        source.AddHook(WndProc);
                };
                bool startHidden = WindowState == WindowState.Minimized;
                startHidden |= Environment.GetCommandLineArgs()
                    .Where(s => !String.IsNullOrEmpty(s)).Select(s => s.ToLower())
                    .Any(s => s.Contains("hidden") || s.Contains("minimized"));
                if (startHidden) {
                    WindowState = WindowState.Minimized;
                    Hide();
                    ShowInTaskbar = false;
                    TrayMenu.Visibility = Visibility.Visible;
                }
            }
            //IDisposable Support
            var dispose = new Action<object, EventArgs>((sender, e) => {
                if (DataContext is IDisposable)
                    (DataContext as IDisposable).Dispose();
            });
            Unloaded += new RoutedEventHandler(dispose);
            Dispatcher.ShutdownStarted += new EventHandler(dispose);
            //Init
            InitializeComponent();
        }
        private static TaskbarIcon TrayMenu {
            get { return Application.Current != null ? ((App)Application.Current).TrayMenu : null; }
        }
        private static NotifyIconViewModel TrayMenuViewModel {
            get { return TrayMenu != null ? (NotifyIconViewModel)TrayMenu.DataContext : null; }
        }
        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("user32.dll")]
        private static extern bool InsertMenu(IntPtr hMenu, Int32 wPosition, Int32 wFlags, Int32 wIdNewItem, [NotNull] string lpNewItem);
        public static void OpenSettings() {
            ICommand command = TrayMenuViewModel.ShowSettingsWindowCommand;
            if (command == null || !command.CanExecute(null)) return;
            command.Execute(null);
        }
        private static void OpenAbout() {
            ICommand command = TrayMenuViewModel.ShowAboutWindowCommand;
            if (command == null || !command.CanExecute(null)) return;
            command.Execute(null);
        }
        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
            if (msg != WmSyscommand) return IntPtr.Zero;
            switch (wParam.ToInt32()) {
                case SettingsSysMenuId:
                    OpenSettings();
                    handled = true;
                    break;
                case AboutSysMenuId:
                    OpenAbout();
                    handled = true;
                    break;
            }
            return IntPtr.Zero;
        }
    }
}