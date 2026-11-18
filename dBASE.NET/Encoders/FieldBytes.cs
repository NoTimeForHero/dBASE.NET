using System;
using System.Text;

namespace dBASE.NET.Encoders
{
    internal static class FieldBytes
    {
        public static bool HasStandardAsciiBytes(Encoding encoding)
        {
            if (encoding == null)
                return false;

            // An application can supply a custom Encoding with the same code page.
            var assembly = encoding.GetType().Assembly;
            bool systemEncoding = assembly == typeof(Encoding).Assembly;
#if NET8_0_OR_GREATER
            systemEncoding |= assembly == typeof(CodePagesEncodingProvider).Assembly;
#endif
            return systemEncoding && encoding.CodePage is 866 or 1251 or 20127 or 65001;
        }

        public static ArraySegment<byte> TrimAsciiSpaces(ArraySegment<byte> field)
        {
            var bytes = field.Array;
            int start = field.Offset;
            int end = start + field.Count;
            while (start < end && bytes[start] == (byte)' ')
                start++;
            while (end > start && bytes[end - 1] == (byte)' ')
                end--;

            return new ArraySegment<byte>(bytes, start, end - start);
        }

        public static bool TryDecodeAsciiInteger(ArraySegment<byte> field, out int value)
        {
            value = 0;
            var bytes = field.Array;
            int start = field.Offset;
            int end = start + field.Count;
            bool negative = bytes[start] == (byte)'-';
            if (negative || bytes[start] == (byte)'+')
                start++;

            // Nine digits always fit in Int32 and are exact in Double.
            int digitCount = end - start;
            if (digitCount < 1 || digitCount > 9)
                return false;

            int magnitude = 0;
            for (int i = start; i < end; i++)
            {
                int digit = bytes[i] - (byte)'0';
                if ((uint)digit > 9)
                    return false;
                magnitude = magnitude * 10 + digit;
            }

            // Parsing signed zero can produce different bit patterns across runtimes.
            if (negative && magnitude == 0)
                return false;

            value = negative ? -magnitude : magnitude;
            return true;
        }

        public static bool IsAscii(ArraySegment<byte> field)
        {
#if NET8_0_OR_GREATER
            return Ascii.IsValid(field.AsSpan());
#else
            var bytes = field.Array;
            int end = field.Offset + field.Count;
            for (int i = field.Offset; i < end; i++)
            {
                if (bytes[i] >= 0x80)
                    return false;
            }
            return true;
#endif
        }

        public static void RequireWidth(ArraySegment<byte> field, int minimumSize)
        {
            if (field.Count < minimumSize)
                throw new ArgumentException($"The DBF field requires at least {minimumSize} bytes.", nameof(field));
        }

        public static byte[] GetFieldArray(ArraySegment<byte> field)
        {
            if (field.Count == field.Array.Length)
                return field.Array;

            var bytes = new byte[field.Count];
            Buffer.BlockCopy(field.Array, field.Offset, bytes, 0, field.Count);
            return bytes;
        }
    }
}
