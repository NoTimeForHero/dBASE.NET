using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace dBASE.NET.Other.Extensions
{
    internal static class BinaryExtensions
    {
        public static int ReadInt32Reverse(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        public static int ReadInt16Reverse(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(2);
            Array.Reverse(bytes);
            return BitConverter.ToInt16(bytes, 0);
        }

        public static uint ReadUInt16Reverse(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(2);
            Array.Reverse(bytes);
            return BitConverter.ToUInt16(bytes, 0);
        }

        public static uint ReadUInt32Reverse(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        // https://stackoverflow.com/a/4074557
        public static T ReadStructure<T>(this BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));

            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();

            return theStructure;
        }

        public static void WriteReverse(this BinaryWriter writer, int value)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            writer.Write(bytes);
        }

        public static void WriteReverse(this BinaryWriter writer, ushort value)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            writer.Write(bytes);
        }
    }
}
