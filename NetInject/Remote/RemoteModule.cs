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
    using System.Runtime.InteropServices;
    using System.Text;

    using Annotations;
    using Interop;
    [DebuggerDisplay("{Name}")]
    public class RemoteModule {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private Kernel32.ImageDosHeader? _dosHeader;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private Kernel32.ImageExportDirectory? _exportDirectory;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private Kernel32.ImageDataDirectory? _exportDirectoryInfo;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private Kernel32.ImageFileHeader? _fileHeader;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private string _filePath;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private RemoteFunction[] _functions;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private Kernel32.Moduleinfo? _info;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private string _name;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private uint? _signature;
        public RemoteModule(RemoteProcess process, IntPtr handle) {
            Process = process;
            Handle = handle;
        }
        public RemoteModule(RemoteProcess process, IntPtr handle, string filePath) {
            Process = process;
            _filePath = filePath.ToLower();
            Handle = handle;
        }
        public RemoteProcess Process { get; private set; }
        public string FilePath {
            get {
                if (_filePath != null) return _filePath;
                try {
                    IntPtr pTargetProcess = Process.Handle.GetAccess(Kernel32.ProcessSecurity.ProcessQueryInformation | Kernel32.ProcessSecurity.ProcessVmRead);
                    var lpFileName = new char[260];
                    uint length = Kernel32.GetModuleFileNameEx(pTargetProcess, Handle, lpFileName, (uint)lpFileName.Length);
                    if (length == 0)
                        throw new Win32Exception();
                    _filePath = Path.GetFullPath(new String(lpFileName, 0, (int)length).ToLower());
                } catch {
                    _filePath = null;
                }
                return _filePath;
            }
        }
        public string Name {
            get {
                if (_name != null) return _name;
                if (FilePath == null) return null;
                string filename = Path.GetFileName(FilePath);
                string extension = Path.GetExtension(filename);
                if (extension.Length > 0)
                    filename = filename.Replace(extension, String.Empty);
                _name = filename;
                return _name;
            }
        }
        public IntPtr Handle { get; private set; }
        public bool IsValid {
            get { return Process.IsValid && FilePath != null && Handle != IntPtr.Zero; }
        }
        private Kernel32.Moduleinfo Info {
            get {
                if (_info != null) return _info.Value;
                Kernel32.Moduleinfo lpmodinfo;
                var cb = (uint)Marshal.SizeOf(typeof(Kernel32.Moduleinfo));
                IntPtr processHandle = Process.Handle.GetAccess(Kernel32.ProcessSecurity.ProcessQueryInformation | Kernel32.ProcessSecurity.ProcessVmRead);
                if (!Kernel32.GetModuleInformation(processHandle, Handle, out lpmodinfo, cb))
                    throw new Win32Exception();
                _info = lpmodinfo;
                return _info.Value;
            }
        }
        [UsedImplicitly]
        public IntPtr BaseAddress {
            get { return Info.lpBaseOfDll; }
        }
        [UsedImplicitly]
        public IntPtr EntryPoint {
            get { return Info.EntryPoint; }
        }
        [UsedImplicitly]
        public Kernel32.ImageDosHeader DosHeader {
            get {
                if (_dosHeader != null) return _dosHeader.Value;
                var header = Process.Memory.Read<Kernel32.ImageDosHeader>(BaseAddress);
                if (!header.IsValid)
                    throw new InvalidDataException("Image DOS Header is corrupted");
                _dosHeader = header;
                return _dosHeader.Value;
            }
        }
        [UsedImplicitly]
        public uint Signature {
            get {
                if (_signature == null)
                    _signature = Process.Memory.Read<uint>(BaseAddress + DosHeader.e_lfanew);
                return _signature.Value;
            }
        }
        [UsedImplicitly]
        public Kernel32.ImageFileHeader FileHeader {
            get {
                if (_fileHeader != null) return _fileHeader.Value;
                var header =
                    Process.Memory.Read<Kernel32.ImageFileHeader>(BaseAddress + DosHeader.e_lfanew + sizeof(uint));
                _fileHeader = header;
                return _fileHeader.Value;
            }
        }
        public bool IsX64 {
            get { return FileHeader.SizeOfOptionalHeader == Marshal.SizeOf(typeof(Kernel32.ImageOptionalHeader64)); }
        }
        [UsedImplicitly]
        public bool IsX86 {
            get { return !IsX64; }
        }
        [UsedImplicitly]
        public Object OptionalHeader {
            get {
                IntPtr pos = BaseAddress + DosHeader.e_lfanew + sizeof(uint) +
                             Marshal.SizeOf(typeof(Kernel32.ImageFileHeader));
                if (IsX64) {
                    var optHeader = Process.Memory.Read<Kernel32.ImageOptionalHeader64>(pos);
                    if (optHeader.Magic != Kernel32.MagicType.ImageNtOptionalHdr64Magic)
                        throw new InvalidDataException(
                            "Header of module is invalid (should be IMAGE_NT_OPTIONAL_HDR64_MAGIC)");
                    return optHeader;
                } else {
                    var optHeader = Process.Memory.Read<Kernel32.ImageOptionalHeader32>(pos);
                    if (optHeader.Magic != Kernel32.MagicType.ImageNtOptionalHdr32Magic)
                        throw new InvalidDataException(
                            "Header of module is invalid (should be IMAGE_NT_OPTIONAL_HDR32_MAGIC)");
                    return optHeader;
                }
            }
        }
        [UsedImplicitly]
        public Kernel32.ImageDataDirectory ExportDirectoryInfo {
            get {
                if (_exportDirectoryInfo != null) return _exportDirectoryInfo.Value;
                var tempExportDirectory = new Kernel32.ImageDataDirectory();
                if (IsX64) {
                    var optHeader = (Kernel32.ImageOptionalHeader64)OptionalHeader;
                    if (optHeader.NumberOfRvaAndSizes > 0) {
                        tempExportDirectory.VirtualAddress = optHeader.ExportTable.VirtualAddress;
                        tempExportDirectory.Size = optHeader.ExportTable.Size;
                    } else
                        throw new InvalidDataException("ExportDirectory invalid (x64)");
                } else {
                    var optHeader = (Kernel32.ImageOptionalHeader32)OptionalHeader;
                    if (optHeader.NumberOfRvaAndSizes > 0) {
                        tempExportDirectory.VirtualAddress = optHeader.ExportTable.VirtualAddress;
                        tempExportDirectory.Size = optHeader.ExportTable.Size;
                    } else
                        throw new InvalidDataException("ExportDirectory invalid (x86)");
                }
                _exportDirectoryInfo = tempExportDirectory;
                return _exportDirectoryInfo.Value;
            }
        }
        [UsedImplicitly]
        public Kernel32.ImageExportDirectory ExportDirectory {
            get {
                if (_exportDirectory == null)
                    _exportDirectory =
                        Process.Memory.Read<Kernel32.ImageExportDirectory>(BaseAddress + (int)ExportDirectoryInfo.VirtualAddress);
                return _exportDirectory.Value;
            }
        }
        [UsedImplicitly]
        public RemoteFunction[] Functions {
            get {
                if (_functions != null)
                    return _functions;
                int addressOfNameOrdinals = (int)ExportDirectory.AddressOfNameOrdinals,
                    addressOfNames = (int)ExportDirectory.AddressOfNames;
                if (addressOfNameOrdinals == 0 || addressOfNames == 0) return new RemoteFunction[0];
                var exportOrdinals =
                    Process.Memory.Read<UInt16[]>(BaseAddress + addressOfNameOrdinals, (int)ExportDirectory.NumberOfNames);
                var exportNames = Process.Memory.Read<int[]>(BaseAddress + addressOfNames, (int)ExportDirectory.NumberOfNames);
                var functionsList = new List<RemoteFunction>((int)ExportDirectory.NumberOfNames);
                for (int i = 0; i < ExportDirectory.NumberOfNames; i++) {
                    IntPtr address = GetFunctionAddress(exportOrdinals[i], i);
                    if (address == IntPtr.Zero) continue;
                    functionsList.Add(new RemoteFunction(this, address, BaseAddress + exportNames[i]));
                }
                return _functions = functionsList.ToArray();
            }
        }
        [UsedImplicitly]
        public RemoteFunction GetCustomFunction(IntPtr address) {
            return new RemoteFunction(this, address, IntPtr.Zero);
        }
        [UsedImplicitly]
        public void Refresh() {
            _functions = null;
            _info = null;
            _dosHeader = null;
            _signature = null;
            _fileHeader = null;
            _exportDirectory = null;
            _exportDirectoryInfo = null;
        }
        [UsedImplicitly]
        public RemoteFunction GetFunction(IntPtr address, string name = "") {
            return Functions.FirstOrDefault(foo => foo.Address == address) ?? new RemoteFunction(this, address, name);
        }
        public RemoteFunction GetFunction(string name) {
            int index = Array.BinarySearch(Functions, name);
            return index >= 0 ? Functions[index] : null;
        }
        [UsedImplicitly]
        public RemoteFunction GetFunctionByOrdinal(uint ordinal) {
            return GetFunctionByOrdinal(ordinal, IntPtr.Zero);
        }
        private RemoteFunction GetFunctionByOrdinal(uint ordinal, IntPtr namePtr) {
            if (ordinal < ExportDirectory.Base || ordinal >= ExportDirectory.Base + ExportDirectory.NumberOfFunctions)
                return null;
            var fooIndex = (int)(ordinal - ExportDirectory.Base); //Actual Ordinal
            IntPtr address = GetFunctionAddress(fooIndex, fooIndex);
            return address != IntPtr.Zero ? (Functions.FirstOrDefault(foo => foo.Address == address) ?? new RemoteFunction(this, address, namePtr)) : null;
        }
        private IntPtr GetFunctionAddress(int ordinal, int nameIndex) {
            var addressOffset =
                Process.Memory.Read<int>(BaseAddress + (int)ExportDirectory.AddressOfFunctions + ordinal * sizeof(int)); //exportFunctions[ordinal];
            if (addressOffset < ExportDirectoryInfo.VirtualAddress || addressOffset >= ExportDirectoryInfo.VirtualAddress + ExportDirectoryInfo.Size)
                return BaseAddress + addressOffset;
            string forwardingString = Process.Memory.ReadString(BaseAddress + addressOffset);
            int dot = forwardingString.IndexOf('.');
            if (dot <= -1) return IntPtr.Zero;
            string redirectModuleName = forwardingString.Substring(0, dot);
            string redirectFunctionName = forwardingString.Substring(dot + 1);
            RemoteModule redirectModule = Process.GetModule(redirectModuleName);
            if (redirectModule == null || redirectModule.BaseAddress == BaseAddress) return IntPtr.Zero;
            if (redirectFunctionName[0] == '#') {
                redirectFunctionName = redirectFunctionName.Remove(0, 1);
                UInt32 redirectOrdinal;
                UInt32.TryParse(redirectFunctionName, out redirectOrdinal);
                if (redirectOrdinal == 0) return IntPtr.Zero;
                IntPtr namePtr = ExportDirectory.AddressOfNames != 0 ? BaseAddress + Process.Memory.Read<int>(BaseAddress + (int)ExportDirectory.AddressOfNames + nameIndex * sizeof(int)) : IntPtr.Zero;
                RemoteFunction foo = redirectModule.GetFunctionByOrdinal(redirectOrdinal, namePtr);
                return foo != null ? foo.Address : IntPtr.Zero;
            } else {
                RemoteFunction foo = redirectModule.GetFunction(redirectFunctionName);
                return foo != null ? foo.Address : IntPtr.Zero;
            }
        }
    }
}