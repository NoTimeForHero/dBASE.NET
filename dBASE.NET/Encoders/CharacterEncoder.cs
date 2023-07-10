namespace dBASE.NET.Encoders
{
    using System;
    using System.Text;
    using System.Collections;
    using System.Collections.Generic;

    internal class CharacterEncoder : IEncoder
    {
        // cach different length bytes (for performance)
        Dictionary<int, byte[]> buffers = new Dictionary<int, byte[]>();

        private byte[] GetBuffer(int length)
        {
            if (!buffers.TryGetValue(length, out var bytes))
            {
                var s = new string(' ', length);
                bytes = Encoding.ASCII.GetBytes(s);
                buffers.Add(length, bytes);
            }
            return (byte[])bytes.Clone();
        }

        /// <inheritdoc />
        public byte[] Encode(DbfField field, object data, Encoding encoding, MemoContext memo)
        {
            // Input data maybe various: int, string, whatever.
            string res = data?.ToString();
            if (string.IsNullOrEmpty(res))
            {
                res = field.DefaultValue;
            }

            // consider multibyte should truncate or padding after GetBytes (11 bytes)
            var buffer = GetBuffer(field.Length);
            var bytes = encoding.GetBytes(res);
            Array.Copy(bytes, buffer, Math.Min(bytes.Length, field.Length));

            return buffer;
        }

        /// <inheritdoc />
        public object Decode(byte[] buffer, Encoding encoding, MemoContext memo)
        {
            string text = encoding.GetString(buffer).Trim();
            if (text.Length == 0) return null;
            return text;
        }
    }
}