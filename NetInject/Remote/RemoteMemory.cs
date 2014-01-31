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
    using System.Runtime.InteropServices;
    using System.Text;
    using Annotations;
    using Interop;
    public class RemoteMemory {
        public RemoteMemory(RemoteProcess process) {
            Process = process;
        }
        /// <summary>
        ///     The BaseProcess that is assigned to the memory
        /// </summary>
        [UsedImplicitly]
        public RemoteProcess Process { get; private set; }
        [UsedImplicitly]
        public bool IsValid {
            get { return Process.IsValid; }
        }
        private static byte ReadByte(IntPtr targetProcess, IntPtr memoryAddress) {
            var result = new byte[1];
            if (!Kernel32.ReadProcessMemory(targetProcess, memoryAddress, result, (UIntPtr)1, IntPtr.Zero))
                throw new Win32Exception();
            return result[0];
        }
        public string ReadString(IntPtr memoryAddress, int maxLength = 0) {
            IntPtr targetProcess = Process.Handle.GetAccess(Kernel32.ProcessSecurity.ProcessVmRead);
            string result;
            if (maxLength == 0)
                try {
                    var bytes = new List<byte>(260);
                    for (byte current = ReadByte(targetProcess, memoryAddress);
                        current != 0;
                        current = ReadByte(targetProcess, memoryAddress += 1))
                        bytes.Add(current);
                    result = Encoding.ASCII.GetString(bytes.ToArray());
                } catch {
                    result = null;
                }
            else {
                IntPtr memPointer = Marshal.AllocHGlobal(maxLength);
                try {
                    UIntPtr length;
                    if (!Kernel32.ReadProcessMemory(targetProcess, memoryAddress, memPointer, (UIntPtr)maxLength,
                        out length))
                        throw new Win32Exception();
                    result = Marshal.PtrToStringAnsi(memPointer);
                } catch {
                    result = null;
                } finally {
                    Marshal.FreeHGlobal(memPointer);
                }
            }
            return result;
        }
        /// <summary>
        ///     Remotely reads data from a memory address
        /// </summary>
        /// <typeparam name="T">The type to cast the data to. Use byte[] if unsure</typeparam>
        /// <param name="memoryAddress"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public T Read<T>(IntPtr memoryAddress, int size = 0) {
            IntPtr targetProcess = Process.Handle.GetAccess(Kernel32.ProcessSecurity.ProcessVmRead);
            if (typeof(T) == typeof(string)) return (T)(object)ReadString(memoryAddress, size);
            UIntPtr length;
            if (size == 0)
                length = (UIntPtr)Marshal.SizeOf(typeof(T));
            else if (typeof(T).IsArray)
                length = (UIntPtr)(size * Marshal.SizeOf(typeof(T).GetElementType()));
            else
                length = (UIntPtr)size;
            var buf = new byte[(int)length];
            if (!Kernel32.ReadProcessMemory(targetProcess, memoryAddress, buf, (UIntPtr)buf.Length, out length))
                throw new Win32Exception();
            return TypeCaster.Cast<T>(buf);
        }
        /// <summary>
        ///     Remotely allocates memory. Throws Win32Exception if it fails.
        /// </summary>
        /// <param name="size">The size in bytes to allocate</param>
        /// <param name="allocationType"></param>
        /// <param name="protect"></param>
        /// <returns>The absolute memory address of the allocated memory</returns>
        public IntPtr Allocate(int size,
            Kernel32.AllocationType allocationType =
                Kernel32.AllocationType.MemCommit | Kernel32.AllocationType.MemReserve,
            Kernel32.Protect protect = Kernel32.Protect.PageExecuteReadwrite) {
            IntPtr targetProcess = Process.Handle.GetAccess(Kernel32.ProcessSecurity.ProcessVmOperation);
            IntPtr processMemory = Kernel32.VirtualAllocEx(targetProcess, IntPtr.Zero, (UIntPtr)size, allocationType,
                protect);
            if (processMemory == IntPtr.Zero)
                throw new Win32Exception();
            return processMemory;
        }
        /// <summary>
        ///     Remotely releases virtual memory. Throws Win32Exception if it fails.
        /// </summary>
        /// <param name="address">The absolute memory address to free</param>
        /// <param name="allocationType">The allocation Type</param>
        public void Release(IntPtr address,
            Kernel32.AllocationType allocationType = Kernel32.AllocationType.MemRelease) {
            IntPtr targetProcess = Process.Handle.GetAccess(Kernel32.ProcessSecurity.ProcessVmOperation);
            if (!Kernel32.VirtualFreeEx(targetProcess, address, UIntPtr.Zero, allocationType))
                throw new Win32Exception();
        }
        /// <summary>
        ///     Remotely writes data to memory address. Throws Win32Exception if it fails.
        ///     If the data is a string, it will assume ASCII Encoding.
        /// </summary>
        /// <typeparam name="T">
        ///     The Data type. string, marshal structs, arrays and basic types allowed. Throws Exception if it
        ///     fails
        /// </typeparam>
        /// <param name="address">The absolute memory address to write to</param>
        /// <param name="data">The data to write</param>
        /// <returns>The amount of bytes that have been written</returns>
        public int Write<T>(IntPtr address, T data) {
            IntPtr targetProcess =
                Process.Handle.GetAccess(Kernel32.ProcessSecurity.ProcessVmOperation |
                                         Kernel32.ProcessSecurity.ProcessVmWrite);
            var dataBytes = TypeCaster.Cast<Byte[]>(data);
            UIntPtr nBytesWritten;
            if (!Kernel32.WriteProcessMemory(targetProcess, address, dataBytes, (UIntPtr)dataBytes.Length,
                out nBytesWritten))
                throw new Win32Exception();
            return (int)nBytesWritten;
        }
    }
}