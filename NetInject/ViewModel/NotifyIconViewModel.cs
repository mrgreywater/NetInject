//
//  Author: gReY
//  Contact: mr.greywater+netinject@gmail.com
//  Software: NetInject
//  This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. 
//  If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
namespace NetInject.ViewModel {
    using System;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media.Imaging;
    using Annotations;
    using Utils.MicroMvvm;
    /// <summary>
    ///     ViewModel of a NotifyIcon which is the Icon you see in the tray
    /// </summary>
    public class NotifyIconViewModel : ObservableObject {
        private BitmapFrame _icon;
        public BitmapFrame Icon {
            get {
                if (_icon != null) return _icon;
                _icon = BitmapFrame.Create(new Uri("pack://application:,,,/Resources/MainIcon.ico", UriKind.RelativeOrAbsolute));
                return _icon;
            }
            set {
                if (Equals(_icon, value)) return;
                _icon = value;
                RaisePropertyChanged("Icon");
            }
        }
        public ICommand ShowWindowCommand {
            get {
                return new RelayCommand(param => {
                    Application.Current.MainWindow.Show();
                    Application.Current.MainWindow.WindowState = WindowState.Normal;
                    Application.Current.MainWindow.Activate();
                });
            }
        }
        public ICommand ShowAboutWindowCommand {
            get {
                return new RelayCommand(param => {
                    if (Application.Current.MainWindow == null || Application.Current.MainWindow.Visibility == Visibility.Hidden)
                        ShowWindowCommand.Execute(null);
                    Window window = Application.Current.MainWindow;
                    bool useTemporaryWindow = window != null && !window.IsVisible;
                    if (useTemporaryWindow) {
                        window = new Window {Visibility = Visibility.Hidden, Width = 0, Height = 0, Left = Int32.MinValue, Top = Int32.MinValue};
                        window.Show();
                    }
                    if (window == null) return;
                    MessageBox.Show(window, "Written by gReY\nContact: mr.greywater+netinject@gmail.com\n\nLicensed under Mozilla Public License Version 2.0\n", "About NetInject", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    if (useTemporaryWindow)
                        window.Close();
                });
            }
        }
        public ICommand ShowSettingsWindowCommand {
            get {
                return new RelayCommand(param => {
                    if (Application.Current == null)
                        return;
                    var settings = new SettingsWindow();
                    if (Application.Current.MainWindow == null || Application.Current.MainWindow.Visibility == Visibility.Hidden)
                        ShowWindowCommand.Execute(null);
                    if (Application.Current.MainWindow != null)
                        settings.Owner = Application.Current.MainWindow;
                    settings.ShowDialog();
                });
            }
        }
        [UsedImplicitly]
        public ICommand ExitApplicationCommand {
            get { return new RelayCommand(param => Application.Current.Shutdown()); }
        }
    }
}