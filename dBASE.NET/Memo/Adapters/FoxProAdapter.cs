using System;
using System.IO;
using System.Text;
using dBASE.NET.Encoders;
using dBASE.NET.Other.Extensions;

namespace dBASE.NET.Memo.Adapters
{
    internal class FoxProAdapter : IMemoAdapter
    {
        private const int headerSize = 1024;
        private const int defaultBlockSize = 64;
        private const int blockMetaSize = 8; // Meta-Information like block end marker
        private const int maxReusableLength = 64 * 1024;
        private const string harbourSignature = "Harbour";

        private BinaryWriter writer;
        private BinaryReader reader;
        private Stream stream;
        private int blockSize;
        private bool preserveWhitespace;
        private byte[] readBuffer = Array.Empty<byte>();

        public void Initialize(Stream stream, BinaryReader reader, BinaryWriter writer)
        {
            this.stream = stream;
            this.reader = reader;
            this.writer = writer;

            if (writer != null && stream.Length < headerSize)
            {
                GenerateHeader(headerSize, defaultBlockSize);
                blockSize = defaultBlockSize;
                preserveWhitespace = true;
            }
            else
            {
                if (stream.Length < 512) throw new InvalidDataException("Incomplete FPT header.");
                stream.Position = 6;
                blockSize = (int)reader.ReadUInt16Reverse();
                if (blockSize == 0) throw new InvalidDataException("Invalid FPT block size.");
                // Keep the established trimming behavior for non-Harbour FPT files.
                preserveWhitespace = Encoding.ASCII.GetString(reader.ReadBytes(harbourSignature.Length)) == harbourSignature;
            }
        }

        private void GenerateHeader(int headerSize, int blockSize)
        {
            writer.Write(new byte[headerSize]);
            stream.Position = 6;
            writer.WriteReverse((ushort)blockSize);
            writer.Write(Encoding.ASCII.GetBytes(harbourSignature));
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
            stream.Seek((long)index * blockSize + 4, SeekOrigin.Begin);
            var length = (int)reader.ReadUInt32Reverse();
            var text = DecodeBlock(length, encoding);
            return preserveWhitespace ? text : text.Trim();
        }

        private string DecodeBlock(int length, Encoding encoding)
        {
            // A custom encoding can override the whole-array overload or retain its input.
            if (length <= 0 || length > maxReusableLength || !encoding.IsReadOnly || !FieldBytes.IsSystemEncoding(encoding))
                return encoding.GetString(reader.ReadBytes(length));

            if (readBuffer.Length < length)
                readBuffer = new byte[Math.Min(maxReusableLength, Math.Max(length, Math.Max(256, readBuffer.Length * 2)))];

            int count = 0;
            while (count < length)
            {
                int read = reader.Read(readBuffer, count, length - count);
                if (read == 0)
                    break;
                count += read;
            }
            return encoding.GetString(readBuffer, 0, count);
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
