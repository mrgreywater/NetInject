//
//  Author: gReY
//  Contact: mr.greywater+netinject@gmail.com
//  Software: NetInject
//  This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. 
//  If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
namespace NetInject {
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using NotifyIcon;
    using Utils;
    /// <summary>
    ///     Main application start and shutdown logic
    /// </summary>
    public partial class App {
        public TaskbarIcon TrayMenu { get; private set; }
        protected override void OnStartup(StartupEventArgs e) {
            //Check if it is VmWare and disable HWAcceleration
            if (HandleWpfHwAcceleration()) {
                Shutdown();
                return;
            }
            base.OnStartup(e);
            //Fatal exception handling
            DispatcherUnhandledException += (sender, args) => {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
                MessageBox.Show(String.Format("{0}:\n\n{1}", args.Exception.Message, GetExceptionMessages(args.Exception.InnerException)), "Uncaught Exception - " + args.Exception.Message,
                    MessageBoxButton.OK);
                args.Handled = true;
                Environment.Exit(1);
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
                var ex = (Exception)args.ExceptionObject;
                MessageBox.Show(String.Format("{0}:\n\n{1}", ex.Message, GetExceptionMessages(ex.InnerException)), "Uncaught Thread Exception - " + ex.Message, MessageBoxButton.OK);
                Environment.Exit(1);
            };
            //Only one instance
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1) {
                MessageBox.Show("Application is already running. If you need multiple instances, "
                                + "create a copy of this executable with another name.", "Notification", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                Current.Shutdown();
            }
            //Tray Menu
            TrayMenu = (TaskbarIcon)FindResource("NotifyIcon");
            Exit += (sender, args) => {
                if (TrayMenu != null)
                    TrayMenu.Dispose();
            };
            if (TrayMenu != null)
                TrayMenu.Visibility = Visibility.Hidden;
        }
        private static string GetExceptionMessages(Exception e) {
            if (e == null) return string.Empty;
            if (e.InnerException != null)
                return String.Format("{0}\n{1}", e.Message, GetExceptionMessages(e.InnerException));
            return e.Message + "\n";
        }
        private static bool HandleWpfHwAcceleration() {
            try {
                string restart = Environment.GetCommandLineArgs()
                    .Where(s => !String.IsNullOrEmpty(s))
                    .Select(s => s.ToLower())
                    .FirstOrDefault(s => s.Contains("/restart"));
                if (String.IsNullOrEmpty(restart)) {
                    bool noHw = Environment.GetCommandLineArgs()
                        .Where(s => !String.IsNullOrEmpty(s)).Select(s => s.ToLower())
                        .Any(s => s.Contains("/nohw")) || VmwareHotfix.IsVm;
                    if (!noHw || !VmwareHotfix.HwAcceleration) return false;
                    VmwareHotfix.HwAcceleration = false;
                    Process.Start(Environment.GetCommandLineArgs()[0], "/restart" + Process.GetCurrentProcess().Id + " /nohw");
                    return true;
                }
                restart = restart.Replace("/restart", String.Empty);
                int id;
                if (!Int32.TryParse(restart, out id)) return false;
                bool isRunning = true;
                while (isRunning) {
                    //Wait for other process to finish
                    Thread.Sleep(100);
                    try {
                        if (Process.GetProcessById(id).HasExited)
                            isRunning = false;
                    } catch {
                        isRunning = false;
                    }
                }
                return false;
            } catch {
                return false;
            }
        }
    }
}