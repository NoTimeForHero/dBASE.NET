using System;

namespace dBASE.NET.Encoders
{
    internal static class FieldBytes
    {
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
