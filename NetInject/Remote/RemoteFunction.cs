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
    using System.IO;
    using System.Text;
    using System.Threading;
    using Annotations;
    using Interop;
    [DebuggerDisplay("{Name}")]
    public class RemoteFunction : IComparable {
        private const Kernel32.ProcessSecurity CreateRemoteThreadSecurityAttributes =
            Kernel32.ProcessSecurity.ProcessCreateThread |
            Kernel32.ProcessSecurity.ProcessQueryInformation |
            Kernel32.ProcessSecurity.ProcessVmOperation |
            Kernel32.ProcessSecurity.ProcessVmWrite |
            Kernel32.ProcessSecurity.ProcessVmRead;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private readonly IntPtr _nameAddress;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private string _name;
        public RemoteFunction(RemoteModule module, IntPtr address, string name) {
            Module = module;
            _name = name;
            Address = address;
        }
        public RemoteFunction(RemoteModule module, IntPtr address, IntPtr functionName) {
            Module = module;
            _nameAddress = functionName;
            Address = address;
        }
        /// <summary>
        ///     The parent module
        /// </summary>
        [UsedImplicitly]
        public RemoteModule Module { get; private set; }
        /// <summary>
        ///     The memory pointer of the unmanaged function
        /// </summary>
        public IntPtr Address { get; private set; }
        /// <summary>
        ///     The exported name of the function
        /// </summary>
        [UsedImplicitly]
        public string Name {
            get { return _name ?? (_name = _nameAddress != IntPtr.Zero ? Module.Process.Memory.ReadString(_nameAddress) : String.Empty); }
        }
        /// <summary>
        ///     Checks if the function is valid. That means, if the parent module, process are still present
        /// </summary>
        [UsedImplicitly]
        public bool IsValid {
            get { return Address != IntPtr.Zero && Module.IsValid; }
        }
        public int CompareTo(object obj) {
            if (obj is string)
                return String.Compare(Name, obj as String, StringComparison.Ordinal);
            throw new ArgumentException("Object is not a String");
        }
        /// <summary>
        ///     A synchronized call of a unmanaged function of another process via CreateRemoteThread.
        /// </summary>
        /// <returns></returns>
        [UsedImplicitly]
        public uint Call() {
            return Call(IntPtr.Zero);
        }
        /// <summary>
        ///     A synchronized call of a unmanaged function of another process via CreateRemoteThread.
        /// </summary>
        /// <param name="argument"></param>
        /// <returns></returns>
        public uint Call(Object argument) {
            byte[] argumentBytes;
            if (argument is string) argumentBytes = Name[Name.Length - 1] == 'W' ? Encoding.Unicode.GetBytes(argument as string) : Encoding.ASCII.GetBytes(argument as string);
            else
                argumentBytes = TypeCaster.Cast<byte[]>(argument);
            if (argumentBytes == null || argumentBytes.Length == 0)
                return Call(IntPtr.Zero);
            IntPtr memoryAddress = Module.Process.Memory.Allocate(argumentBytes.Length);
            uint result;
            try {
                if (Module.Process.Memory.Write(memoryAddress, argumentBytes) != argumentBytes.Length)
                    throw new InvalidDataException("Data size mismatch, argument not correctly written to memory. Remote function call failed");
                result = Call(memoryAddress);
            } finally {
                Module.Process.Memory.Release(memoryAddress);
            }
            return result;
        }
        /// <summary>
        ///     A synchronized call of a unmanaged function of another process via CreateRemoteThread.
        /// </summary>
        /// <param name="argument"></param>
        /// <returns></returns>
        public uint Call(IntPtr argument) {
            IntPtr targetProcess = Module.Process.Handle.GetAccess(CreateRemoteThreadSecurityAttributes);
            uint threadId, returnCode;
            IntPtr remoteThread = Kernel32.CreateRemoteThread(targetProcess, IntPtr.Zero, UIntPtr.Zero, Address, argument, 0, out threadId);
            if (remoteThread == IntPtr.Zero)
                throw new Win32Exception();
            try {
                Kernel32.SetThreadPriority(remoteThread, Kernel32.Priority.ThreadPriorityHighest); //Don't care if it fails
                if (Kernel32.WaitForSingleObject(remoteThread, Timeout.Infinite) == -1)
                    throw new Win32Exception();
                if (!Kernel32.GetExitCodeThread(remoteThread, out returnCode))
                    throw new Win32Exception();
            } finally {
                Kernel32.CloseHandle(remoteThread); // Don't care if it fails
            }
            return returnCode;
        }
    }
}