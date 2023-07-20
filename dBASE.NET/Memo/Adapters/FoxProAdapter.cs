using System;
using System.IO;
using System.Text;
using dBASE.NET.Other.Extensions;

namespace dBASE.NET.Memo.Adapters
{
    internal class FoxProAdapter : IMemoAdapter
    {
        private const int headerSize = 1024;
        private const int defaultBlockSize = 64;
        private const int blockMetaSize = 8; // Meta-Information like block end marker

        private BinaryWriter writer;
        private BinaryReader reader;
        private Stream stream;
        private int blockSize;

        public void Initialize(Stream stream, BinaryReader reader, BinaryWriter writer)
        {
            this.stream = stream;
            this.reader = reader;
            this.writer = writer;

            if (stream.Length < headerSize)
            {
                GenerateHeader(headerSize, defaultBlockSize);
                blockSize = defaultBlockSize;
            }
            else
            {
                stream.Position = 6;
                blockSize = (int)reader.ReadUInt16Reverse();
            }
        }

        private void GenerateHeader(int headerSize, int blockSize)
        {
            writer.Write(new byte[headerSize]);
            stream.Position = 6;
            writer.WriteReverse((ushort)blockSize);
            writer.Write(Encoding.ASCII.GetBytes("Harbour"));
            stream.Position = 512;
            writer.Write(Encoding.ASCII.GetBytes("FlexFile3"));
            stream.Position = headerSize;
            SetFreeBlock(headerSize / blockSize); // No idea why
        }

        public string GetBlockData(int index, Encoding encoding)
        {
            // https://www.clicketyclick.dk/databases/xbase/format/fpt.html#FPT_STRUCT
            // [0 - 3 bytes] - Record type
            // [4 - 7 bytes] - Length of memo field
            // [8 - ...n ] - Memo data
            // Block
            stream.Seek(index * blockSize + 4, SeekOrigin.Begin);
            var length = (int)reader.ReadUInt32Reverse();
            var buffer = reader.ReadBytes(length);
            return encoding.GetString(buffer).Trim();
        }

        public BlockWriteStatusEnum WriteBlockData(int index, byte[] data)
        {
            stream.Seek(index * blockSize + 4, SeekOrigin.Begin);
            var oldLength = (int)reader.ReadUInt32Reverse();
            if (data.Length > blockSize - blockMetaSize)
            {
                int increasedBy = data.Length - oldLength;
                int canBeAdded = Utils.LeftSizeInBlock(blockSize, oldLength + blockMetaSize);
                if (increasedBy > canBeAdded) return BlockWriteStatusEnum.NeedResize;
            }

            stream.Seek(index * blockSize + 4, SeekOrigin.Begin);
            writer.WriteReverse(data.Length);
            writer.Write(data);
            return BlockWriteStatusEnum.Success;
        }

        public int AppendBlock(byte[] data)
        {
            int sizeInBlocks = Utils.BlocksNeededToFit(blockSize, data.Length + blockMetaSize);
            var index = GetFreeBlock();

            var totalLength = (index + sizeInBlocks) * blockSize;
            stream.SetLength(totalLength);

            stream.Seek(index * blockSize + 4, SeekOrigin.Begin);
            writer.WriteReverse(data.Length);
            writer.Write(data);
            SetFreeBlock(index + sizeInBlocks);
            return index;
        }

        private int GetFreeBlock()
        {
            stream.Position = 0;
            return (int)reader.ReadUInt32Reverse();
        }

        private void SetFreeBlock(int value)
        {
            stream.Position = 0;
            writer.WriteReverse(value);
        }
    }
}
