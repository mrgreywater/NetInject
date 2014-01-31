//
//  Author: gReY
//  Contact: mr.greywater+netinject@gmail.com
//  Software: NetInject
//  This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. 
//  If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
namespace NetInject.Remote {
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using Annotations;
    using Interop;
    /// <summary>
    ///     Class allowing to list and load/unload dlls into other processes, call unmanaged functions and access their memory.
    ///     Works both for x86 and x64 processes
    /// </summary>
    [DebuggerDisplay("{ProcessName}")]
    public class RemoteProcess {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private readonly Process _baseProcess;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private readonly ProcessHandle _handle;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private RemoteMemory _memory;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private RemoteModule[] _modules;
        /// <summary>
        ///     Creates a new RemoteProcess from a System.Diagnostics.BaseProcess
        /// </summary>
        /// <param name="baseProcess"></param>
        public RemoteProcess(Process baseProcess) {
            _baseProcess = baseProcess;
            _handle = new ProcessHandle(baseProcess);
        }
        /// <summary>
        ///     Creates a new RemoteProcess from a name. Throws an exception if the BaseProcess wasn't found.
        /// </summary>
        /// <param name="id"></param>
        public RemoteProcess(int id) {
            Process process = Process.GetProcessById(id);
            _baseProcess = process;
            _handle = new ProcessHandle(process);
        }
        /// <summary>
        ///     Creates a new RemoteProcess from a name. Throws an exception if the BaseProcess wasn't found
        /// </summary>
        /// <param name="name"></param>
        public RemoteProcess(string name) {
            Process[] processes = Process.GetProcessesByName(name);
            if (processes.Length > 0)
                _baseProcess = processes[0];
            else
                throw new EntryPointNotFoundException("BaseProcess " + name + " does not exist");
            _handle = new ProcessHandle(_baseProcess);
        }
        public Process BaseProcess {
            get { return _baseProcess; }
        }
        /// <summary>
        ///     The BaseProcess Name of the BaseProcess
        /// </summary>
        [UsedImplicitly]
        public string ProcessName {
            get { return _baseProcess.ProcessName; }
        }
        /// <summary>
        ///     The BaseProcess Id of the BaseProcess.
        /// </summary>
        [UsedImplicitly]
        public int Id {
            get { return _baseProcess.Id; }
        }
        /// <summary>
        ///     The loaded Modules of the BaseProcess. Can be cleared with Refresh()
        /// </summary>
        [UsedImplicitly]
        public RemoteModule[] Modules {
            get {
                if (_modules != null)
                    return _modules;
                try {
                    _baseProcess.WaitForInputIdle();
                } catch (Exception e) {
                    Debug.WriteLine(e.Message);
                }
                if (Environment.OSVersion.Version.Major < 6) {
                    ProcessModuleCollection nativeModules = _baseProcess.Modules;
                    var modulesList = new List<RemoteModule>(nativeModules.Count);
                    modulesList.AddRange(from ProcessModule nativeModule in nativeModules
                        let moduleHandle = Kernel32.GetModuleHandle(nativeModule.FileName)
                        where moduleHandle != IntPtr.Zero
                        select new RemoteModule(this, moduleHandle, nativeModule.ModuleName));
                    _modules = modulesList.ToArray();
                } else {
                    IntPtr targetProcess =
                        Handle.GetAccess(Kernel32.ProcessSecurity.ProcessQueryInformation |
                                         Kernel32.ProcessSecurity.ProcessVmRead);
                    IntPtr[] modulePointers = null;
                    for (UInt32 cb = 0, neededCb = 1024 * (uint)IntPtr.Size; cb < neededCb;) {
                        cb = neededCb;
                        modulePointers = new IntPtr[neededCb];
                        if (!Kernel32.EnumProcessModulesEx(targetProcess, modulePointers, cb, out neededCb,
                            Kernel32.ModuleFilter.ListModulesAll))
                            throw new Win32Exception();
                    }
                    if (modulePointers == null) return _modules;
                    var modulesList = new List<RemoteModule>(modulePointers.Length);
                    modulesList.AddRange(
                        modulePointers.Where(moduleHandle => moduleHandle != IntPtr.Zero)
                            .Select(moduleHandle => new RemoteModule(this, moduleHandle)));
                    _modules = modulesList.ToArray();
                }
                return _modules;
            }
        }
        /// <summary>
        ///     The Memory of the BaseProcess
        /// </summary>
        public RemoteMemory Memory {
            get { return _memory ?? (_memory = new RemoteMemory(this)); }
        }
        /// <summary>
        ///     The Handle to the BaseProcess
        /// </summary>
        public ProcessHandle Handle {
            get { return _handle; }
        }
        [UsedImplicitly]
        public bool IsX64 {
            get {
                RemoteModule module = GetModule(ProcessName);
                return module != null && module.IsX64;
            }
        }
        [UsedImplicitly]
        public bool IsX86 {
            get { return !IsX64; }
        }
        /// <summary>
        ///     Returns false if the BaseProcess has been closed
        /// </summary>
        public bool IsValid {
            get { return !_baseProcess.HasExited; }
        }
        /// <summary>
        ///     Gets a module via it's name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public RemoteModule GetModule(string name) {
            name = Path.GetFileName(name.ToLower());
            string extension = Path.GetExtension(name);
            if (extension.Length > 0)
                name = name.Replace(extension, String.Empty);
            return Modules.FirstOrDefault(remoteModule => remoteModule.Name == name);
        }
        /// <summary>
        ///     Gets a Module via handle
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        [UsedImplicitly]
        public RemoteModule GetModule(IntPtr handle) {
            var module = new RemoteModule(this, handle);
            if (!module.IsValid)
                throw new DllNotFoundException("Invalid Module Handle");
            return module;
        }
        /// <summary>
        ///     Refreshes the BaseProcess data (modules, memory etc)
        /// </summary>
        [UsedImplicitly]
        public void Refresh() {
            _baseProcess.Refresh();
            _memory = null;
            _modules = null;
        }
        /// <summary>
        ///     Injects a module, throws an exception on failure.
        /// </summary>
        /// <param name="filePath">The filepath of the dynamic link library.</param>
        /// <returns></returns>
        public RemoteModule InjectModule(string filePath) {
            filePath = Path.GetFullPath(filePath);
            if (!File.Exists(filePath))
                throw new FileNotFoundException(filePath + " could not be found");
            RemoteModule kernel32Module = GetModule("kernel32");
            if (kernel32Module == null)
                throw new DllNotFoundException("Module 'kernel32' not found!");
            RemoteFunction loadLibraryW = kernel32Module.GetFunction("LoadLibraryW");
            if (loadLibraryW == null || loadLibraryW.Address == IntPtr.Zero)
                throw new MissingMethodException("Function 'LoadLibraryW' not found in kernel32");
            try {
                if (loadLibraryW.Call(filePath) == 0)
                    throw new MethodAccessException("LoadLibrary call failed. This usually means that you tried to inject a incompatible 32 bit dll in a 64 bit Process, or vice versa.");
            } catch (Exception e) {
                throw new MethodAccessException("Method LoadLibraryW could not be called!", e);
            }
            Refresh();
            return GetModule(filePath);
        }
        /// <summary>
        ///     Tries to inject a module.
        /// </summary>
        /// <param name="filePath">The file path of the dynamic link library.</param>
        /// <returns>True if the module was injected successfully</returns>
        [UsedImplicitly]
        public RemoteModule TryInjectModule(string filePath) {
            RemoteModule module;
            try {
                module = InjectModule(filePath);
            } catch {
                module = null;
            }
            return module;
        }
        /// <summary>
        ///     Ejects a specific module, throws an exception on failure.
        /// </summary>
        /// <param name="module"></param>
        [UsedImplicitly]
        public void EjectModule(RemoteModule module) {
            if (module == null || !module.IsValid)
                throw new DllNotFoundException("Module is invalid!");
            IntPtr moduleHandle = module.Handle;
            RemoteModule kernel32Module = GetModule("kernel32");
            if (kernel32Module == null)
                throw new DllNotFoundException("Module 'kernel32' not found!");
            RemoteFunction freeLibrary = kernel32Module.GetFunction("FreeLibrary");
            if (freeLibrary == null)
                throw new MissingMethodException("Function 'FreeLibrary' not found in kernel32");
            try {
                if (freeLibrary.Call(moduleHandle) == 0)
                    throw new MethodAccessException("FreeLibrary call failed. This usually means that you tried to eject a non existent Module");
            } catch (Exception e) {
                throw new MethodAccessException("Method FreeLibrary could not be called!", e);
            }
            Refresh();
        }
        /// <summary>
        ///     Tries to eject a specific module
        /// </summary>
        /// <param name="module"></param>
        /// <returns>true if the module was ejected, false otherwise</returns>
        [UsedImplicitly]
        public bool TryEjectModule(RemoteModule module) {
            bool success = true;
            try {
                EjectModule(module);
            } catch {
                success = false;
            }
            return success;
        }
        /// <summary>
        ///     Forces a module to unload by calling FreeLibrary until the module is ejected
        /// </summary>
        /// <param name="name"></param>
        /// <returns>false if the module could not be ejected and is still loaded by the BaseProcess</returns>
        public bool ForceEjectModule(string name) {
            RemoteModule module;
            bool eject = true;
            while (eject && (module = GetModule(name)) != null) eject = TryEjectModule(module);
            return eject;
        }
    }
}