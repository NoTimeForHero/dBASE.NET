using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public static uint ReadUInt16Reverse(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(2);
            Array.Reverse(bytes);
            return BitConverter.ToUInt16(bytes, 0);
        }

        public static uint ReadUInt32Reverse(this BinaryReader reader)
        {
            uint value = reader.ReadUInt32();
            return (value >> 24) | ((value >> 8) & 0x0000ff00) |
                ((value << 8) & 0x00ff0000) | (value << 24);
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
