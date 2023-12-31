﻿using System.Linq;
using dBASE.NET.Memo.Adapters;

namespace dBASE.NET.Encoders
{
    using System;
    using System.Text;

    internal class MemoEncoder : IEncoder
    {
        private static readonly object Key = new();

        /// <inheritdoc />
        public byte[] Encode(EncoderContext encoder, object data)
        {
            if (data is not string text) throw new ArgumentException("Memo field value must be a string!");
            var bytes = encoder.Encoding.GetBytes(text);

            byte[] GetBlockIndexData(int block)
            {
                var chars = block.ToString().PadLeft(10);
                return encoder.Encoding.GetBytes(chars);
            }

            bool isPacking = encoder.Memo.PackerInstance != null;

            if (!encoder.RecordContext.TryGetValue(Key, out var ctxObj))
            {
                // If input text is empty  do not create new block in memo...
                if (bytes.Length == 0) return new byte[encoder.Field.Length];

                var target = isPacking ? encoder.Memo.PackerInstance.Adapter : encoder.Memo.Adapter;
                var block = target.AppendBlock(bytes);
                return GetBlockIndexData(block);
            }

            if (ctxObj is ContextData ctxData)
            {
                if (isPacking)
                {
                    var block = encoder.Memo.PackerInstance.Adapter.AppendBlock(bytes);
                    return GetBlockIndexData(block);
                }
                if (ctxData.blockIndex == ContextData.MissingBlockIndex) return ctxData.inputBuffer;
                var status = encoder.Memo.Adapter.WriteBlockData(ctxData.blockIndex, bytes);
                if (status == BlockWriteStatusEnum.NeedResize)
                {
                    var block = encoder.Memo.Adapter.AppendBlock(bytes);
                    return GetBlockIndexData(block);
                }
                return ctxData.inputBuffer;
            }

            throw new InvalidOperationException("Invalid context data type!");
        }

        /// <inheritdoc />
        public object Decode(EncoderContext encoder, byte[] buffer)
        {
            var index = GetBlockIndex(buffer, encoder.Encoding);
            encoder.RecordContext[Key] = new ContextData { inputBuffer = buffer, blockIndex = index ?? ContextData.MissingBlockIndex };
            if (!index.HasValue) return null;
            return encoder.Memo.Adapter.GetBlockData(index.Value, encoder.Encoding);
        }

        private static int? GetBlockIndex(byte[] buffer, Encoding encoding)
        {
            int index;
            // Memo fields of 5+ byts in length store their index in text, e.g. "     39394"
            // Memo fields of 4 bytes store their index as an int.
            if (buffer.Length > 4)
            {
                string text = Encoding.ASCII.GetString(buffer).Trim();
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