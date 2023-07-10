namespace dBASE.NET.Encoders
{
    using System;
    using System.Text;

    internal class MemoEncoder : IEncoder
    {
        public static readonly object Key = new();

        /// <inheritdoc />
        public byte[] Encode(EncoderContext encoder, object data)
        {
            if (!encoder.RecordContext.TryGetValue(Key, out var ctxObj))
                throw new InvalidOperationException("Missing context data with block index and length!");
            if (ctxObj is not ContextData ctxData) throw new InvalidOperationException("Invalid context data type!");

            if (ctxData.blockIndex == ContextData.MissingBlockIndex) return ctxData.inputBuffer;

            var bytes = encoder.Encoding.GetBytes(data.ToString());
            encoder.Memo.WriteBlockData(ctxData.blockIndex, bytes);
            return ctxData.inputBuffer;
        }

        /// <inheritdoc />
        public object Decode(EncoderContext encoder, byte[] buffer)
        {
            var index = GetBlockIndex(buffer, encoder.Encoding);
            encoder.RecordContext[Key] = new ContextData { inputBuffer = buffer, blockIndex = index ?? ContextData.MissingBlockIndex };
            if (!index.HasValue) return null;
            return encoder.Memo.GetBlockData(index.Value, encoder.Encoding);
        }

        private static int? GetBlockIndex(byte[] buffer, Encoding encoding)
        {
            int index;
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

        private struct ContextData
        {
            public const int MissingBlockIndex = -1;

            public byte[] inputBuffer;
            public int blockIndex;
        }
    }
}