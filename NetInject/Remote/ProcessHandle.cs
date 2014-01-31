//
//  Author: gReY
//  Contact: mr.greywater+netinject@gmail.com
//  Software: NetInject
//  This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. 
//  If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
namespace NetInject.Remote {
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using Annotations;
    using Interop;
    [DebuggerDisplay("{_currentFlags}")]
    public class ProcessHandle {
        private Kernel32.ProcessSecurity _currentFlags = 0;
        private IntPtr _currentHandle = IntPtr.Zero;
        public ProcessHandle(Process process) {
            try {
                Process.EnterDebugMode(); /*"To open a handle to another local process and obtain full access rights, you must enable the SeDebugPrivilege privilege"*/
                IsDebugMode = true;
            } catch {
                IsDebugMode = false;
            }
            Process = process;
        }
        [UsedImplicitly]
        public bool IsDebugMode { get; private set; }
        [UsedImplicitly]
        public Process Process { get; private set; }
        ~ProcessHandle() {
            CloseHandle();
        }
        public IntPtr GetAccess(Kernel32.ProcessSecurity securityFlags) {
            if ((_currentFlags & securityFlags) == securityFlags)
                return _currentHandle;
            CloseHandle();
            _currentHandle = Kernel32.OpenProcess(_currentFlags |= securityFlags, false, Process.Id);
            if (_currentHandle == IntPtr.Zero)
                throw new Win32Exception();
            return _currentHandle;
        }
        private void CloseHandle() {
            if (_currentHandle == IntPtr.Zero) return;
            Kernel32.CloseHandle(_currentHandle);
            _currentHandle = IntPtr.Zero;
        }
    }
}