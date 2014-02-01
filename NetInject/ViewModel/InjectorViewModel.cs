//
//  Author: gReY
//  Contact: mr.greywater+netinject@gmail.com
//  Software: NetInject
//  This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. 
//  If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
namespace NetInject.ViewModel {
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Annotations;
    using Microsoft.Win32;
    using Remote;
    using Utils;
    using Utils.MicroMvvm;
    /// <summary>
    ///     ViewModel of an Injector that allows to disable and enable modules, and keeps track of the running applications and
    ///     the module files
    /// </summary>
    internal class InjectorViewModel : ObservableObject, IDisposable {
        private static readonly RegistryKey RegistryIndex = Registry.CurrentUser.CreateSubKey("Software").CreateSubKey("NetInject").CreateSubKey(Process.GetCurrentProcess().ProcessName);
        private readonly BitmapFrame _colorIcon = BitmapFrame.Create(new Uri("pack://application:,,,/Resources/MainIconColor.ico", UriKind.RelativeOrAbsolute));
        private readonly BitmapFrame _defaultIcon = BitmapFrame.Create(new Uri("pack://application:,,,/Resources/MainIcon.ico", UriKind.RelativeOrAbsolute));
        private readonly FileSystemWatcher _fileWatcher = new FileSystemWatcher();
        private readonly TaskFactory _tasks = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
        private RemoteProcess _activeProcess;
        private string _applicationName;
        private string _extension;
        private ObservableCollection<ModuleViewModel> _modules = new ObservableCollection<ModuleViewModel>();
        public InjectorViewModel() {
            Extension = (RegistryIndex.GetValue("Extension") ?? "dll").ToString();
            ApplicationName = (RegistryIndex.GetValue("Application") ?? "notepad.exe").ToString();
            //Set up ProcessWatcher and Search through Processes
            var processWatcher = new Thread(ApplicationStartTask) {IsBackground = true, Name = "ProcessWatcher"};
            processWatcher.Start();
            //Set up FileWatcher
            _fileWatcher.Path = Directory.GetCurrentDirectory();
            _fileWatcher.Filter = "*.*";
            _fileWatcher.IncludeSubdirectories = true;
            _fileWatcher.Changed += OnFileChanged;
            _fileWatcher.Created += OnFileChanged;
            _fileWatcher.Deleted += OnFileChanged;
            _fileWatcher.Renamed += OnFileChanged;
            _fileWatcher.EnableRaisingEvents = true;
            Icon = _defaultIcon;
        }
        public string Header {
            get { return "NetInject: " + ApplicationName; }
        }
        public ImageSource Icon {
            get { return Application.Current != null ? (((App)Application.Current).TrayMenu.IconSource) : _defaultIcon; }
            set {
                _tasks.StartNew(() => {
                    if (Equals(value, Icon) || Application.Current == null) return;
                    ((App)Application.Current).TrayMenu.IconSource = value;
                    RaisePropertyChanged("Icon");
                });
            }
        }
        [UsedImplicitly]
        public RemoteProcess ActiveProcess {
            get {
                if (_activeProcess == null)
                    return null;
                if (_activeProcess.BaseProcess.HasExited)
                    ActiveProcess = null;
                return _activeProcess;
            }
            private set {
                if (value == _activeProcess) return;
                _activeProcess = value;
                Icon = value == null ? _defaultIcon : _colorIcon;
                _tasks.StartNew(() => Modules.ForEach(module => module.BaseProcess = value));
                RaisePropertyChanged("ActiveProcess");
            }
        }
        public string ApplicationName {
            get { return _applicationName; }
            set {
                value = (value ?? String.Empty).Replace(".exe", String.Empty);
                if (value == _applicationName) return;
                _applicationName = value;
                ActiveProcess = null;
                RaisePropertyChanged("ApplicationName");
                RaisePropertyChanged("Header");
            }
        }
        public string Extension {
            get { return _extension; }
            set {
                if (value == _extension || value == null || value.Length > 260) return;
                _extension = value;
                ActiveProcess = null; //Lazy Code: Unloads all Modules
                Modules.Clear();
                Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*." + Extension, SearchOption.AllDirectories).ForEach(AddModule);
                RaisePropertyChanged("Extension");
            }
        }
        public ObservableCollection<ModuleViewModel> Modules {
            get { return _modules; }
        }
        private bool Disposed { get; set; }
        [UsedImplicitly]
        public ICommand ShowSettingsWindowCommand {
            get { return new RelayCommand(o => MainWindow.OpenSettings()); }
        }
        [UsedImplicitly]
        public ICommand SelectAll {
            get { return new RelayCommand(o => Modules.ForEach(module => module.Activated = true), o => Modules.Any(module => !module.Activated)); }
        }
        [UsedImplicitly]
        public ICommand SelectNone {
            get { return new RelayCommand(o => Modules.ForEach(module => module.Activated = false), o => Modules.Any(module => module.Activated)); }
        }
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private static void SetPropertiesFromRegistry(ModuleViewModel module) {
            module.Activated = false;
            module.IsError = false;
            RegistryKey registryKey = RegistryIndex.CreateSubKey("Modules");
            if (registryKey == null) return;
            object val = registryKey.GetValue(module.FilePath);
            if (!(val is string)) return;
            bool registryValue;
            if (!Boolean.TryParse(val as string, out registryValue)) return;
            if (registryValue)
                module.Activated = true;
            else
                module.IsError = true;
        }
        private void SaveStateToRegistry() {
            try {
                if (Extension != null)
                    RegistryIndex.SetValue("Extension", Extension);
                if (ApplicationName != null)
                    RegistryIndex.SetValue("Application", ApplicationName);
                RegistryKey registryModulesKey = RegistryIndex.CreateSubKey("Modules");
                if (registryModulesKey == null) return;
                registryModulesKey.GetValueNames().ForEach(registryModulesKey.DeleteValue);
                Modules.Where(module => module != null && module.Activated).ForEach(module => registryModulesKey.SetValue(module.FilePath, true));
                Modules.Where(module => module != null && module.IsError).ForEach(module => registryModulesKey.SetValue(module.FilePath, false));
            } catch (Exception e) {
                Debug.WriteLine(e.Message);
            }
        }
        private bool IsValidExtension(string comparison) {
            return String.Equals(Path.GetExtension(comparison) ?? "", "." + Extension, StringComparison.OrdinalIgnoreCase);
        }
        private static bool PathEquals(string source, string comparison) {
            source = Path.GetFullPath(new Uri(source).LocalPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            comparison = Path.GetFullPath(new Uri(comparison).LocalPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return String.Equals(source, comparison, StringComparison.OrdinalIgnoreCase);
        }
        private void ApplicationStartTask() {
            for (; !Disposed; Thread.Sleep(1000)) {
                if (ActiveProcess != null || ApplicationName == null)
                    continue;
                Process runningProcess = Process.GetProcessesByName(ApplicationName).OrderBy(p => p.StartTime).FirstOrDefault();
                if (runningProcess == null)
                    continue;
                ActiveProcess = new RemoteProcess(runningProcess);
                ActiveProcess.BaseProcess.Exited += OnApplicationEnd;
                ActiveProcess.BaseProcess.EnableRaisingEvents = true;
            }
        }
        private void OnApplicationEnd(object sender, EventArgs e) {
            // It's _activeProcess to make sure not to call the getter method
            if (_activeProcess == null || sender != _activeProcess.BaseProcess) return;
            _activeProcess.BaseProcess.EnableRaisingEvents = false;
            ActiveProcess = null;
        }
        private void ReloadModule(string filePath) {
            if (!IsValidExtension(filePath))
                return;
            Modules.Where(module => module.Activated && PathEquals(module.FilePath, filePath)).ForEach(module => module.Reload());
        }
        private void AddModule(string filePath) {
            if (!IsValidExtension(filePath))
                return;
            _tasks.StartNew(() => {
                var module = new ModuleViewModel(filePath);
                Modules.Add(module);
                Modules.Sort(o => o);
                SetPropertiesFromRegistry(module);
                module.BaseProcess = ActiveProcess;
            });
        }
        private void RemoveModule(string filePath) {
            if (!IsValidExtension(filePath))
                return;
            _tasks.StartNew(() => Modules.Where(module => PathEquals(module.FilePath, filePath)).ToList().ForEach(module => {
                module.Activated = false;
                module.BaseProcess = ActiveProcess;
                Modules.Remove(module);
            }));
        }
        private void OnFileChanged(object source, FileSystemEventArgs e) {
            switch (e.ChangeType) {
                case WatcherChangeTypes.Renamed:
                    if (e is RenamedEventArgs)
                        RemoveModule((e as RenamedEventArgs).OldFullPath);
                    AddModule(e.FullPath);
                    break;
                case WatcherChangeTypes.Changed:
                    ReloadModule(e.FullPath);
                    break;
                case WatcherChangeTypes.Created:
                    AddModule(e.FullPath);
                    break;
                case WatcherChangeTypes.Deleted:
                    RemoveModule(e.FullPath);
                    break;
                default:
                    Debug.WriteLine("Did not expect File " + e.FullPath + " Type:" + e.ChangeType);
                    break;
            }
        }
        ~InjectorViewModel() {
            Dispose(false);
        }
        private void Dispose(bool disposing) {
            if (Disposed) return;
            if (disposing) //Managed Resources
                if (_fileWatcher != null) {
                    _fileWatcher.EnableRaisingEvents = false;
                    _fileWatcher.Dispose();
                }
            //Unmanaged Resources
            SaveStateToRegistry();
            ActiveProcess = null;
            _applicationName = null;
            _extension = null;
            _modules = null;
            Disposed = true;
        }
    }
}