//
//  Author: gReY
//  Contact: mr.greywater+netinject@gmail.com
//  Software: NetInject
//  This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. 
//  If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
namespace NetInject.Remote {
    using System;
    using System.Runtime.InteropServices;
    using System.Text;
    public static class TypeCaster {
        /// <summary>
        ///     Slow but convenient Function to Marshal structures, arrays and other types between each other.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="structure"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static T Cast<T>(Object structure, int offset = 0) {
            Type inputT = structure.GetType();
            Object output;
            if (inputT == typeof(T))
                output = structure;
            else {
                byte[] input = inputT == typeof(String)
                    ? Encoding.ASCII.GetBytes(offset > 0 ? ((string)structure).Substring(offset) : (string)structure)
                    : StructToByteArray(structure, offset);
                output = ByteArrayToStruct(input, 0, typeof(T));
            }
            return (T)output;
        }
        private static byte[] StructToByteArray(Object structure, int offset) {
            int size;
            byte[] array;
            if (structure.GetType().IsArray) {
                int memberSize = Marshal.SizeOf(structure.GetType().GetElementType());
                var input = (Array)structure;
                size = input.Length * memberSize;
                if (structure.GetType() != typeof(byte[])) {
                    array = new byte[size];
                    for (int i = 0; i < input.Length; i++) {
                        byte[] byteValues = StructToByteArray(input.GetValue(i), 0);
                        byteValues.CopyTo(array, i * byteValues.Length);
                    }
                } else
                    array = (byte[])structure;
                if (offset <= 0) return array;
                var tmp = new byte[size - offset];
                Array.Copy(array, offset, tmp, 0, size - offset);
                array = tmp;
            } else {
                size = Marshal.SizeOf(structure);
                array = new byte[size];
                IntPtr buffer = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(structure, buffer, true);
                Marshal.Copy(buffer, array, offset, size - offset);
                Marshal.FreeHGlobal(buffer);
            }
            return array;
        }
        private static Object ByteArrayToStruct(byte[] inputArray, int offset, Type structType) {
            Object result;
            if (structType.IsArray)
                if (structType != typeof(byte[])) {
                    Type memberType = structType.GetElementType();
                    int memberSize = Marshal.SizeOf(memberType);
                    int outputSize = inputArray.Length / memberSize;
                    Array arr = Array.CreateInstance(memberType, outputSize);
                    for (int i = 0; i < outputSize; i++) {
                        object val = ByteArrayToStruct(inputArray, i * memberSize, memberType);
                        arr.SetValue(val, i);
                    }
                    result = arr;
                } else
                    result = inputArray;
            else {
                int size = Marshal.SizeOf(structType);
                if (inputArray.Length < offset + size)
                    throw new ArgumentException("Input exceeds output range");
                byte[] tmp;
                if (size != inputArray.Length) {
                    tmp = new byte[size];
                    Array.Copy(inputArray, offset, tmp, 0, size);
                } else
                    tmp = inputArray;
                GCHandle structHandle = GCHandle.Alloc(tmp, GCHandleType.Pinned);
                object structure = Marshal.PtrToStructure(structHandle.AddrOfPinnedObject(), structType);
                structHandle.Free();
                result = structure;
            }
            return result;
        }
    }
}