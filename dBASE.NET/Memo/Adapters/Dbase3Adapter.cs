 using System;
using System.IO;
using System.Text;
using dBASE.NET.Encoders;

namespace dBASE.NET.Memo.Adapters
{
    internal class Dbase3Adapter : IMemoAdapter
    {
        private Stream stream;
        private const int blockMetaSize = 2; // Meta-Information like block end marker
        private const int blockSize = 512;
        private const int maxReusableLength = 64 * 1024;
        const byte markerBlockEnd = 0x1A; // 0x1A/26 - block end marker
        private byte[] readBuffer = new byte[blockSize];

        public void Initialize(Stream stream, BinaryReader reader, BinaryWriter writer)
        {
            this.stream = stream;
            var len = stream.Length;

            if (len < blockSize && (len > 0 || writer == null))
                throw new InvalidDataException("Corrupted header!");
            if (len == 0) GenerateHeader();
        }

        public string GetBlockData(int index, Encoding encoding)
        {
            var data = ReadBlock(index, out int length);
            stream.Seek((long)blockSize * index + length, SeekOrigin.Begin);
            if (encoding.IsReadOnly && FieldBytes.IsSystemEncoding(encoding))
                return encoding.GetString(data, 0, length).Trim();

            var exactData = new byte[length];
            Buffer.BlockCopy(data, 0, exactData, 0, length);
            return encoding.GetString(exactData).Trim();
        }

        public BlockWriteStatusEnum WriteBlockData(int index, byte[] data)
        {
            long position = stream.Position;
            ReadBlock(index, out int oldLength);
            stream.Position = position;
            if (data.Length > blockSize - blockMetaSize)
            {
                int increasedBy = data.Length - oldLength;
                int canBeAdded = Utils.LeftSizeInBlock(blockSize, oldLength + blockMetaSize);
                if (increasedBy > canBeAdded) return BlockWriteStatusEnum.NeedResize;
            }

            var offset = (long)blockSize * index;
            stream.Seek(offset, SeekOrigin.Begin);

            stream.Write(data, 0, data.Length);
            stream.WriteByte(markerBlockEnd);
            stream.WriteByte(markerBlockEnd);
            return BlockWriteStatusEnum.Success;
        }

        public int AppendBlock(byte[] data)
        {
            int sizeInBlocks = Utils.BlocksNeededToFit(blockSize, data.Length + blockMetaSize);
            var block = GetFreeBlock();

            var newBlockLen = data.Length + blockMetaSize;
            long offset = (long)block * blockSize;
            stream.SetLength(offset + newBlockLen);
            stream.Seek(offset, SeekOrigin.Begin);

            stream.Write(data, 0, data.Length);
            stream.WriteByte(markerBlockEnd);
            stream.WriteByte(markerBlockEnd);

            SetFreeBlock(block + sizeInBlocks);
            return block;
        }

        private byte[] ReadBlock(int index, out int length)
        {
            stream.Seek((long)blockSize * index, SeekOrigin.Begin);
            var data = readBuffer;
            int count = 0;
            while (true)
            {
                if (count == data.Length)
                {
                    Array.Resize(ref data, checked(data.Length * 2));
                    if (data.Length <= maxReusableLength)
                        readBuffer = data;
                }

                int read = stream.Read(data, count, Math.Min(blockSize, data.Length - count));
                if (read == 0)
                    throw new InvalidDataException("Block does not contain an end marker.");

                int end = count + read;
                for (int i = Math.Max(1, count); i < end; i++)
                {
                    if (data[i] == markerBlockEnd && data[i - 1] == markerBlockEnd)
                    {
                        length = i - 1;
                        return data;
                    }
                }
                count = end;
            }
        }

        private void GenerateHeader()
        {
            var header = new byte[512];
            stream.Write(header, 0, header.Length);
            SetFreeBlock(1);
            stream.Seek(5, SeekOrigin.Begin);
            stream.WriteByte(2);
            stream.Seek(8, SeekOrigin.Begin);
            var title = Encoding.ASCII.GetBytes("Harbour");
            stream.Write(title, 0, title.Length);
        }

        private int GetFreeBlock()
        {
            var position = stream.Position;
            stream.Seek(0, SeekOrigin.Begin);
            var buffer = new byte[4];
            var _ = stream.Read(buffer, 0, buffer.Length);
            var number = BitConverter.ToInt32(buffer, 0);
            stream.Position = position;
            return number;
        }

        private void SetFreeBlock(int value)
        {
            var position = stream.Position;
            stream.Seek(0, SeekOrigin.Begin);
            var buffer = BitConverter.GetBytes(value);
            stream.Write(buffer, 0, buffer.Length);
            stream.Position = position;
        }
    }
}
