namespace dBASE.NET.Encoders
{
    using System;
    using System.Text;
    using System.Collections;
    using System.Collections.Generic;

    internal class CharacterEncoder : IEncoder
    {
        // cach different length bytes (for performance)
        Dictionary<int, byte[]> buffers = new();

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
        public byte[] Encode(EncoderContext context, object data)
        {
            // Input data maybe various: int, string, whatever.
            string res = data?.ToString();
            if (string.IsNullOrEmpty(res))
            {
                res = context.Field.DefaultValue;
            }

            // consider multibyte should truncate or padding after GetBytes (11 bytes)
            var buffer = GetBuffer(context.Field.Length);
            var bytes = context.Encoding.GetBytes(res);
            Array.Copy(bytes, buffer, Math.Min(bytes.Length, context.Field.Length));

            return buffer;
        }

        /// <inheritdoc />
        public object Decode(EncoderContext context, ArraySegment<byte> field)
        {
            var encoding = context.Encoding;
            // Preserve the input range for custom decoders and configurable fallbacks.
            if (field.Count != 0 && context.CanDecodeAsciiFields && encoding.IsReadOnly &&
                (encoding.CodePage is 866 or 1251 || encoding.DecoderFallback is DecoderReplacementFallback))
            {
#if NET8_0_OR_GREATER
                field = TrimTextPadding(field);
#else
                field = FieldBytes.TrimAsciiSpaces(field);
#endif
                if (field.Count == 0)
                    return null;
            }

            string text = encoding.GetString(field.Array, field.Offset, field.Count).Trim();
            if (text.Length == 0) return null;
            return text;
        }

#if NET8_0_OR_GREATER
        private static ArraySegment<byte> TrimTextPadding(ArraySegment<byte> field)
        {
            if (field.Count < 32)
                return FieldBytes.TrimAsciiSpaces(field);

            ReadOnlySpan<byte> contents = field.AsSpan();
            int first = contents.IndexOfAnyExcept((byte)' ');
            if (first < 0)
                return new ArraySegment<byte>(field.Array, field.Offset + field.Count, 0);

            int length = contents.Slice(first).LastIndexOfAnyExcept((byte)' ') + 1;
            return new ArraySegment<byte>(field.Array, field.Offset + first, length);
        }
#endif
    }
}
