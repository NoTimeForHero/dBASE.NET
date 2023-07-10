namespace dBASE.NET.Encoders
{
    using System;
    using System.Text;

    internal class MemoEncoder : IEncoder
    {
        /// <inheritdoc />
        public byte[] Encode(DbfField field, object data, Encoding encoding, MemoContext memo)
        {
            return null;
        }

        /// <inheritdoc />
        public object Decode(byte[] buffer, Encoding encoding, MemoContext memo)
        {
            var index = GetBlockIndex(buffer, encoding);
            if (!index.HasValue) return null;
            return memo.GetBlockData(index.Value, encoding);
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