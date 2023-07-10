namespace dBASE.NET.Encoders
{
    using System;
    using System.Text;

    internal class MemoEncoder : IEncoder
    {
        public static readonly object Key = new();

        /// <inheritdoc />
        public byte[] Encode(EncoderContext context, object data)
        {
            return new byte[]{};
        }

        /// <inheritdoc />
        public object Decode(EncoderContext context, byte[] buffer)
        {
            var index = GetBlockIndex(buffer, context.Encoding);
            if (!index.HasValue) return null;
            return context.Memo.GetBlockData(index.Value, context.Encoding);
        }

        private static int? GetBlockIndex(byte[] buffer, Encoding encoding)
        {
            int index = 0;
            // Memo fields of 5+ byts in length store their index in text, e.g. "     39394"
            // Memo fields of 4 bytes store their index as an int.
            if (buffer.Length > 4)
            {
                string text = encoding.GetString(buffer).Trim();
                if (text.Length == 0) return null;
                index = Convert.ToInt32(text);
            }
            else
            {
                index = BitConverter.ToInt32(buffer, 0);
                if (index == 0) return null;
            }
            return index;
        }
    }
}