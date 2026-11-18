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
