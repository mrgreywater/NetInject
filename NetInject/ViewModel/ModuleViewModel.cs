//
//  Author: gReY
//  Contact: mr.greywater+netinject@gmail.com
//  Software: NetInject
//  This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. 
//  If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
namespace NetInject.ViewModel {
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Threading;
    using Annotations;
    using Remote;
    using Utils.MicroMvvm;
    /// <summary>
    ///     ViewModel of a RemoteModule which is a library in an application
    /// </summary>
    internal class ModuleViewModel : ObservableObject, IComparable {
        private readonly Action _activateAction;
        private readonly Action _deactivateAction;
        private bool _activated;
        private RemoteProcess _baseProcess;
        private string _filePath;
        private bool _isError;
        private bool _isLoading;
        public ModuleViewModel(string path) {
            FilePath = Path.GetFullPath(path);
            _activateAction = () => {
                try {
                    if (!Activated) {
                        InjectionState = State.None;
                        return;
                    }
                    if (BaseProcess == null) return;
                    Module = BaseProcess.InjectModule(FilePath);
                    InjectionState = State.Success;
                } catch {
                    _activated = false;
                    RaisePropertyChanged("Activated");
                    RaisePropertyChanged("ActivateCommandText");
                    InjectionState = State.Error;
                }
            };
            _deactivateAction = () => {
                try {
                    if (Module == null || !Module.IsValid) {
                        Module = null;
                        return;
                    }
                    Module.Process.TryEjectModule(Module); //Try it once with the handle
                    if (!Module.Process.ForceEjectModule(Module.FilePath)) //In case it was loaded multiple times
                        throw new DllNotFoundException();
                    Module = null;
                    InjectionState = State.None;
                } catch {
                    InjectionState = State.Error;
                }
            };
        }
        private State InjectionState {
            set {
                switch (value) {
                    case State.Error:
                        IsError = true;
                        IsLoading = false;
                        break;
                    case State.Loading:
                        IsLoading = true;
                        break;
                    case State.Success:
                        IsError = false;
                        IsLoading = false;
                        break;
                    default:
                        IsLoading = false;
                        break;
                }
            }
        }
        [UsedImplicitly]
        public bool IsLoading {
            [UsedImplicitly] get { return _isLoading; }
            set {
                if (value == _isLoading) return;
                _isLoading = value;
                RaisePropertyChanged("IsLoading");
            }
        }
        [UsedImplicitly]
        public bool IsError {
            get { return _isError; }
            set {
                if (_isError == value) return;
                _isError = value;
                RaisePropertyChanged("IsError");
            }
        }
        [UsedImplicitly]
        public RemoteProcess BaseProcess {
            get { return _baseProcess; }
            set {
                if (value == _baseProcess) return;
                _baseProcess = value;
                RaisePropertyChanged("BaseProcess");
                Reload();
            }
        }
        public bool Activated {
            get { return _activated; }
            set {
                if (value == _activated) return;
                _activated = value;
                RaisePropertyChanged("Activated");
                RaisePropertyChanged("ActivateCommandText");
                if (value) Activate();
                else Deactivate();
            }
        }
        private RemoteModule Module { get; set; }
        public string FilePath {
            get { return _filePath; }
            set {
                if (_filePath == value) return;
                _filePath = value;
                RaisePropertyChanged("FilePath");
                RaisePropertyChanged("Name");
            }
        }
        public string Name {
            get { return Path.GetFileName(FilePath); }
        }
        [UsedImplicitly]
        public ICommand ShowInExplorer {
            get {
                return new RelayCommand(param => {
                    var info = new ProcessStartInfo {
                        FileName = "explorer",
                        Arguments = string.Format("/e, /select, \"{0}\"", Path.GetFullPath(FilePath))
                    };
                    Process.Start(info);
                }, param => FilePath != null && File.Exists(Path.GetFullPath(FilePath)));
            }
        }
        [UsedImplicitly]
        public ICommand ShowSettingsWindowCommand {
            get { return new RelayCommand(param => MainWindow.OpenSettings()); }
        }
        [UsedImplicitly]
        public string ActivateCommandText {
            get { return Activated ? "Deactivate" : "Activate"; }
        }
        [UsedImplicitly]
        public ICommand ActivateCommand {
            get { return new RelayCommand(o => Activated = !Activated); }
        }
        public int CompareTo(object obj) {
            if (obj is ModuleViewModel) return String.Compare(Name, (obj as ModuleViewModel).Name, StringComparison.CurrentCulture);
            throw new NotSupportedException();
        }
        private static void RunAsync(Action foo) {
            if (Application.Current != null && Application.Current.Dispatcher != null)
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, foo);
            else
                foo();
        }
        private void Activate() {
            InjectionState = State.Loading;
            if (BaseProcess == null) return;
            RunAsync(_activateAction);
        }
        private void Deactivate() {
            InjectionState = State.None;
            if (Module == null) return;
            InjectionState = State.Loading;
            RunAsync(_deactivateAction);
        }
        public void Reload() {
            InjectionState = State.Loading;
            RunAsync(() => {
                _deactivateAction();
                _activateAction();
            });
        }
        //ShowPropertiesCommand
        private enum State {
            None,
            Loading,
            Error,
            Success
        }
    }
}