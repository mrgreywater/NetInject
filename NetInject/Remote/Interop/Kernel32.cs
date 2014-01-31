//
//  Author: gReY
//  Contact: mr.greywater+netinject@gmail.com
//  Thanks @http://www.pinvoke.net/ for some of the struct definitions
//  Software: NetInject
//  This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. 
//  If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable UnusedMember.Global
namespace NetInject.Remote.Interop {
    using System;
    using System.Runtime.InteropServices;
    public static class Kernel32 {
        [Flags]
        public enum AllocationType : uint {
            MemCommit = 0x00001000,
            MemReserve = 0x00002000,
            MemDecommit = 0x4000,
            MemRelease = 0x8000,
            MemReset = 0x00080000,
            MemResetUndo = 0x1000000,
            MemLargePages = 0x20000000,
            MemPhysical = 0x00400000,
            MemTopDown = 0x00100000,
        }
        public enum DllCharacteristicsType : ushort {
            Res0 = 0x0001,
            Res1 = 0x0002,
            Res2 = 0x0004,
            Res3 = 0x0008,
            ImageDllCharacteristicsDynamicBase = 0x0040,
            ImageDllCharacteristicsForceIntegrity = 0x0080,
            ImageDllCharacteristicsNxCompat = 0x0100,
            ImageDllcharacteristicsNoIsolation = 0x0200,
            ImageDllcharacteristicsNoSeh = 0x0400,
            ImageDllcharacteristicsNoBind = 0x0800,
            Res4 = 0x1000,
            ImageDllcharacteristicsWdmDriver = 0x2000,
            ImageDllcharacteristicsTerminalServerAware = 0x8000
        }
        [Flags]
        public enum FileMapAccess : uint {
            FileMapCopy = 0x0001,
            FileMapWrite = 0x0002,
            FileMapRead = 0x0004,
            FileMapAllAccess = 0x001f,
            FileMapExecute = 0x0020,
        }
        public enum MachineType : ushort {
            Native = 0,
            I386 = 0x014c,
            Itanium = 0x0200,
            X64 = 0x8664
        }
        public enum MagicType : ushort {
            ImageNtOptionalHdr32Magic = 0x10b,
            ImageNtOptionalHdr64Magic = 0x20b
        }
        [Flags]
        public enum ModuleFilter : uint {
            ListModules32Bit = 1,
            ListModules64Bit = 2,
            ListModulesAll = 3,
            ListModulesDefault = 0,
        }
        [Flags]
        public enum Priority {
            ThreadModeBackgroundBegin = 0x00010000,
            ThreadModeBackgroundEnd = 0x00020000,
            ThreadPriorityAboveNormal = 1,
            ThreadPriorityBelowNormal = -1,
            ThreadPriorityHighest = 2,
            ThreadPriorityIdle = -15,
            ThreadPriorityLowest = -2,
            ThreadPriorityNormal = 0,
            ThreadPriorityTimeCritical = 15,
        }
        [Flags]
        public enum ProcessSecurity : uint {
            ProcessCreateProcess = 0x0080,
            ProcessCreateThread = 0x0002,
            ProcessDupHandle = 0x0040,
            ProcessQueryInformation = 0x0400,
            ProcessQueryLimitedInformation = 0x1000,
            ProcessSetInformation = 0x0200,
            ProcessSetQuota = 0x0100,
            ProcessSuspendResume = 0x0800,
            ProcessTerminate = 0x0001,
            ProcessVmOperation = 0x0008,
            ProcessVmRead = 0x0010,
            ProcessVmWrite = 0x0020,
            Synchronize = 0x00100000,
        }
        [Flags]
        public enum Protect : uint {
            PageExecute = 0x10,
            PageExecuteRead = 0x20,
            PageExecuteReadwrite = 0x40,
            PageExecuteWritecopy = 0x80,
            PageNoaccess = 0x01,
            PageReadonly = 0x02,
            PageReadwrite = 0x04,
            PageWritecopy = 0x08,
            PageGuard = 0x100,
            PageNocache = 0x200,
            PageWritecombine = 0x400,
        }
        public enum SubSystemType : ushort {
            ImageSubsystemUnknown = 0,
            ImageSubsystemNative = 1,
            ImageSubsystemWindowsGui = 2,
            ImageSubsystemWindowsCui = 3,
            ImageSubsystemPosixCui = 7,
            ImageSubsystemWindowsCeGui = 9,
            ImageSubsystemEfiApplication = 10,
            ImageSubsystemEfiBootServiceDriver = 11,
            ImageSubsystemEfiRuntimeDriver = 12,
            ImageSubsystemEfiRom = 13,
            ImageSubsystemXbox = 14
        }
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        public static extern IntPtr OpenProcess([In] ProcessSecurity dwDesiredAccess,
            [In, MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, [In] int dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle([In] IntPtr hObject);
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        public static extern IntPtr GetProcAddress([In] IntPtr hModule, [In] string lpProcName);
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        public static extern IntPtr GetModuleHandle([In, Optional] string lpModuleName);
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        public static extern IntPtr VirtualAllocEx([In] IntPtr hProcess, [In, Optional] IntPtr lpAddress,
            [In] UIntPtr dwSize, [In] AllocationType flAllocationType, [In] Protect flProtect);
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WriteProcessMemory([In] IntPtr hProcess, [In] IntPtr lpBaseAddress, [In] byte[] buffer,
            [In] UIntPtr size, [Out] out UIntPtr lpNumberOfBytesWritten);
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadProcessMemory([In] IntPtr hProcess, [In] IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer, [In] UIntPtr dwSize, [Out] out UIntPtr lpNumberOfBytesRead);
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadProcessMemory([In] IntPtr hProcess, [In] IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer, [In] UIntPtr dwSize, [Out] IntPtr lpNumberOfBytesRead);
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadProcessMemory([In] IntPtr hProcess, [In] IntPtr lpBaseAddress,
            [Out, MarshalAs(UnmanagedType.AsAny)] object lpBuffer, [In] UIntPtr dwSize,
            [Out] out UIntPtr lpNumberOfBytesRead);
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadProcessMemory([In] IntPtr hProcess, [In] IntPtr lpBaseAddress,
            [Out] IntPtr lpBuffer, [In] UIntPtr dwSize, [Out] out UIntPtr lpNumberOfBytesRead);
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadProcessMemory([In] IntPtr hProcess, [In] IntPtr lpBaseAddress,
            [Out] IntPtr lpBuffer, [In] UIntPtr dwSize, [Out] IntPtr lpNumberOfBytesRead);
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        public static extern IntPtr CreateRemoteThread([In] IntPtr hProcess, [In] IntPtr lpThreadAttribute,
            [In] UIntPtr dwStackSize, [In] IntPtr lpStartAddress, [In] IntPtr lpParameter, [In] UInt32 dwCreationFlags,
            [Out] out uint lpThreadId);
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        public static extern IntPtr MapViewOfFile([In] IntPtr hFileMappingObject, [In] FileMapAccess dwDesiredAccess,
            [In] UInt32 dwFileOffsetHigh, [In] UInt32 dwFileOffsetLow, [In] UIntPtr dwNumberOfBytesToMap);
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        public static extern int WaitForSingleObject([In] IntPtr hHandle, [In] int dwMilliseconds);
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetExitCodeThread([In] IntPtr hThread, [Out] out UInt32 lpExitCode);
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool VirtualFreeEx([In] IntPtr hProcess, [In] IntPtr lpAddress, [In] UIntPtr dwSize,
            [In] AllocationType dwFreeType);
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWow64Process([In] IntPtr processHandle,
            [Out, MarshalAs(UnmanagedType.Bool)] out bool wow64Process);
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetThreadPriority([In] IntPtr hThread, [In] Priority nPriority);
        [DllImport("psapi.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumProcessModulesEx([In] IntPtr hProcess, [Out] IntPtr[] lphModule, [In] UInt32 cb,
            [Out] out UInt32 lpcbNeeded, [In] ModuleFilter dwFilterFlag);
        [DllImport("psapi.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        public static extern uint GetModuleFileNameEx([In] IntPtr hProcess, [In, Optional] IntPtr hModule,
            [Out] char[] lpFileName, [In] uint nSize);
        [DllImport("psapi.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetModuleInformation([In] IntPtr hProcess, [In] IntPtr hModule,
            [Out] out Moduleinfo lpmodinfo, [In] uint cb);
        [StructLayout(LayoutKind.Sequential)]
        public struct ImageDataDirectory {
            public UInt32 VirtualAddress;
            public UInt32 Size;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct ImageDosHeader {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public char[] e_magic; // Magic number
            public UInt16 e_cblp; // Bytes on last page of file
            public UInt16 e_cp; // Pages in file
            public UInt16 e_crlc; // Relocations
            public UInt16 e_cparhdr; // Size of header in paragraphs
            public UInt16 e_minalloc; // Minimum extra paragraphs needed
            public UInt16 e_maxalloc; // Maximum extra paragraphs needed
            public UInt16 e_ss; // Initial (relative) SS value
            public UInt16 e_sp; // Initial SP value
            public UInt16 e_csum; // Checksum
            public UInt16 e_ip; // Initial IP value
            public UInt16 e_cs; // Initial (relative) CS value
            public UInt16 e_lfarlc; // File address of relocation table
            public UInt16 e_ovno; // Overlay number
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public UInt16[] e_res1; // Reserved words
            public UInt16 e_oemid; // OEM identifier (for e_oeminfo)
            public UInt16 e_oeminfo; // OEM information; e_oemid specific
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)] public UInt16[] e_res2; // Reserved words
            public Int32 e_lfanew; // File address of new exe header
            private string EMagic {
                get { return new string(e_magic); }
            }
            public bool IsValid {
                get { return EMagic == "MZ"; }
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct ImageExportDirectory {
            public UInt32 Characteristics;
            public UInt32 TimeDateStamp;
            public UInt16 MajorVersion;
            public UInt16 MinorVersion;
            public UInt32 Name;
            public UInt32 Base;
            public UInt32 NumberOfFunctions;
            public UInt32 NumberOfNames;
            public UInt32 AddressOfFunctions; // RVA from base of image
            public UInt32 AddressOfNames; // RVA from base of image
            public UInt32 AddressOfNameOrdinals; // RVA from base of image
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct ImageFileHeader {
            public UInt16 Machine;
            public UInt16 NumberOfSections;
            public UInt32 TimeDateStamp;
            public UInt32 PointerToSymbolTable;
            public UInt32 NumberOfSymbols;
            public UInt16 SizeOfOptionalHeader;
            public UInt16 Characteristics;
        }
        [StructLayout(LayoutKind.Explicit)]
        public struct ImageOptionalHeader32 {
            [FieldOffset(0)] public MagicType Magic;
            [FieldOffset(2)] public byte MajorLinkerVersion;
            [FieldOffset(3)] public byte MinorLinkerVersion;
            [FieldOffset(4)] public uint SizeOfCode;
            [FieldOffset(8)] public uint SizeOfInitializedData;
            [FieldOffset(12)] public uint SizeOfUninitializedData;
            [FieldOffset(16)] public uint AddressOfEntryPoint;
            [FieldOffset(20)] public uint BaseOfCode;

            // PE32 contains this additional field
            [FieldOffset(24)] public uint BaseOfData;
            [FieldOffset(28)] public uint ImageBase;
            [FieldOffset(32)] public uint SectionAlignment;
            [FieldOffset(36)] public uint FileAlignment;
            [FieldOffset(40)] public ushort MajorOperatingSystemVersion;
            [FieldOffset(42)] public ushort MinorOperatingSystemVersion;
            [FieldOffset(44)] public ushort MajorImageVersion;
            [FieldOffset(46)] public ushort MinorImageVersion;
            [FieldOffset(48)] public ushort MajorSubsystemVersion;
            [FieldOffset(50)] public ushort MinorSubsystemVersion;
            [FieldOffset(52)] public uint Win32VersionValue;
            [FieldOffset(56)] public uint SizeOfImage;
            [FieldOffset(60)] public uint SizeOfHeaders;
            [FieldOffset(64)] public uint CheckSum;
            [FieldOffset(68)] public SubSystemType Subsystem;
            [FieldOffset(70)] public DllCharacteristicsType DllCharacteristics;
            [FieldOffset(72)] public uint SizeOfStackReserve;
            [FieldOffset(76)] public uint SizeOfStackCommit;
            [FieldOffset(80)] public uint SizeOfHeapReserve;
            [FieldOffset(84)] public uint SizeOfHeapCommit;
            [FieldOffset(88)] public uint LoaderFlags;
            [FieldOffset(92)] public uint NumberOfRvaAndSizes;
            [FieldOffset(96)] public ImageDataDirectory ExportTable;
            [FieldOffset(104)] public ImageDataDirectory ImportTable;
            [FieldOffset(112)] public ImageDataDirectory ResourceTable;
            [FieldOffset(120)] public ImageDataDirectory ExceptionTable;
            [FieldOffset(128)] public ImageDataDirectory CertificateTable;
            [FieldOffset(136)] public ImageDataDirectory BaseRelocationTable;
            [FieldOffset(144)] public ImageDataDirectory Debug;
            [FieldOffset(152)] public ImageDataDirectory Architecture;
            [FieldOffset(160)] public ImageDataDirectory GlobalPtr;
            [FieldOffset(168)] public ImageDataDirectory TLSTable;
            [FieldOffset(176)] public ImageDataDirectory LoadConfigTable;
            [FieldOffset(184)] public ImageDataDirectory BoundImport;
            [FieldOffset(192)] public ImageDataDirectory IAT;
            [FieldOffset(200)] public ImageDataDirectory DelayImportDescriptor;
            [FieldOffset(208)] public ImageDataDirectory CLRRuntimeHeader;
            [FieldOffset(216)] public ImageDataDirectory Reserved;
        }
        [StructLayout(LayoutKind.Explicit)]
        public struct ImageOptionalHeader64 {
            [FieldOffset(0)] public MagicType Magic;
            [FieldOffset(2)] public byte MajorLinkerVersion;
            [FieldOffset(3)] public byte MinorLinkerVersion;
            [FieldOffset(4)] public uint SizeOfCode;
            [FieldOffset(8)] public uint SizeOfInitializedData;
            [FieldOffset(12)] public uint SizeOfUninitializedData;
            [FieldOffset(16)] public uint AddressOfEntryPoint;
            [FieldOffset(20)] public uint BaseOfCode;
            [FieldOffset(24)] public ulong ImageBase;
            [FieldOffset(32)] public uint SectionAlignment;
            [FieldOffset(36)] public uint FileAlignment;
            [FieldOffset(40)] public ushort MajorOperatingSystemVersion;
            [FieldOffset(42)] public ushort MinorOperatingSystemVersion;
            [FieldOffset(44)] public ushort MajorImageVersion;
            [FieldOffset(46)] public ushort MinorImageVersion;
            [FieldOffset(48)] public ushort MajorSubsystemVersion;
            [FieldOffset(50)] public ushort MinorSubsystemVersion;
            [FieldOffset(52)] public uint Win32VersionValue;
            [FieldOffset(56)] public uint SizeOfImage;
            [FieldOffset(60)] public uint SizeOfHeaders;
            [FieldOffset(64)] public uint CheckSum;
            [FieldOffset(68)] public SubSystemType Subsystem;
            [FieldOffset(70)] public DllCharacteristicsType DllCharacteristics;
            [FieldOffset(72)] public ulong SizeOfStackReserve;
            [FieldOffset(80)] public ulong SizeOfStackCommit;
            [FieldOffset(88)] public ulong SizeOfHeapReserve;
            [FieldOffset(96)] public ulong SizeOfHeapCommit;
            [FieldOffset(104)] public uint LoaderFlags;
            [FieldOffset(108)] public uint NumberOfRvaAndSizes;
            [FieldOffset(112)] public ImageDataDirectory ExportTable;
            [FieldOffset(120)] public ImageDataDirectory ImportTable;
            [FieldOffset(128)] public ImageDataDirectory ResourceTable;
            [FieldOffset(136)] public ImageDataDirectory ExceptionTable;
            [FieldOffset(144)] public ImageDataDirectory CertificateTable;
            [FieldOffset(152)] public ImageDataDirectory BaseRelocationTable;
            [FieldOffset(160)] public ImageDataDirectory Debug;
            [FieldOffset(168)] public ImageDataDirectory Architecture;
            [FieldOffset(176)] public ImageDataDirectory GlobalPtr;
            [FieldOffset(184)] public ImageDataDirectory TLSTable;
            [FieldOffset(192)] public ImageDataDirectory LoadConfigTable;
            [FieldOffset(200)] public ImageDataDirectory BoundImport;
            [FieldOffset(208)] public ImageDataDirectory IAT;
            [FieldOffset(216)] public ImageDataDirectory DelayImportDescriptor;
            [FieldOffset(224)] public ImageDataDirectory CLRRuntimeHeader;
            [FieldOffset(232)] public ImageDataDirectory Reserved;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct Moduleinfo {
            public IntPtr lpBaseOfDll;
            public uint SizeOfImage;
            public IntPtr EntryPoint;
        }
    }
}