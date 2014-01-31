//
//  Author: gReY
//  Contact: mr.greywater+netinject@gmail.com
//  Software: NetInject
//  This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. 
//  If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
namespace NetInject.ViewModel {
    using System;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;
    using Utils.MicroMvvm;
    /// <summary>
    ///     The ViewModel of the settings of the Injector
    /// </summary>
    internal class SettingsViewModel : ObservableObject {
        private string _applicationName;
        private string _extension;
        public SettingsViewModel() {
            if (Parent == null) return;
            ApplicationName = Parent.ApplicationName;
            Extension = Parent.Extension;
        }
        private static InjectorViewModel Parent {
            get {
                Application app = Application.Current;
                if (app == null || app.MainWindow == null || app.MainWindow.DataContext == null)
                    return null;
                return app.MainWindow.DataContext as InjectorViewModel;
            }
        }
        private bool IsValid { get; set; }
        public string ApplicationName {
            get { return _applicationName; }
            set {
                if (value == _applicationName || value == null) return;
                _applicationName = value;
                IsValid = IsValidFileName(ApplicationName) && IsValidFileName(Extension);
                RaisePropertyChanged("ApplicationName");
            }
        }
        public string Extension {
            get { return _extension; }
            set {
                if (value == _extension || value == null) return;
                _extension = value;
                IsValid = IsValidFileName(ApplicationName) && IsValidFileName(Extension);
                RaisePropertyChanged("Extension");
            }
        }
        public ICommand Apply {
            get {
                return new RelayCommand(o => {
                    Parent.Extension = Extension;
                    Parent.ApplicationName = ApplicationName;
                }, o => IsValid && Parent != null);
            }
        }
        public ICommand Ok {
            get {
                return new RelayCommand(o => {
                    if (!Apply.CanExecute(o) || !Cancel.CanExecute(o)) return;
                    Apply.Execute(o);
                    Cancel.Execute(o);
                }, o => Apply.CanExecute(o) && Cancel.CanExecute(o));
            }
        }
        public static ICommand Cancel {
            get {
                return new RelayCommand(o => {
                    if (o is Window)
                        (o as Window).Close();
                });
            }
        }
        private static bool IsValidFileName(string fileName) {
            return !String.IsNullOrEmpty(fileName) && fileName.Length <= 260 && !Path.GetInvalidFileNameChars().Any(fileName.Contains);
        }
    }
}