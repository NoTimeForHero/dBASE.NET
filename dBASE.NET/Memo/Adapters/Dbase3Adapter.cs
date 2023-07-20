 using System;
using System.IO;
using System.Text;

namespace dBASE.NET.Memo.Adapters
{
    internal class Dbase3Adapter : IMemoAdapter
    {
        private Stream stream;
        private int blockSize = 512;
        const byte markerBlockEnd = 0x1A; // 0x1A/26 - block end marker

        public void Initialize(Stream stream, BinaryReader reader, BinaryWriter writer)
        {
            this.stream = stream;
            var len = stream.Length;

            if (len > 0 && len < blockSize) throw new InvalidDataException("Corrupted header!");
            if (len == 0) GenerateHeader();
        }

        public string GetBlockData(int index, Encoding encoding)
        {
            GotoBlock(index);
            int length = GetBlockContentSize(index);
            var data = new byte[length];
            var _ = stream.Read(data, 0, data.Length);
            return encoding.GetString(data).Trim();
        }

        public BlockWriteStatusEnum WriteBlockData(int index, byte[] data)
        {
            int oldLength = GetBlockContentSize(index);
            if (data.Length > blockSize - 2) // TODO: Write test if data.Length == blockSize
            {
                int increasedBy = data.Length - oldLength;
                int canBeAdded = LeftSizeInBlock(blockSize, oldLength);
                if (increasedBy > canBeAdded) return BlockWriteStatusEnum.NeedResize;
            }

            var offset = blockSize * index;
            stream.Seek(offset, SeekOrigin.Begin);

            stream.Write(data, 0, data.Length);
            stream.WriteByte(markerBlockEnd);
            stream.WriteByte(markerBlockEnd);
            return BlockWriteStatusEnum.Success;
        }

        public int AppendBlock(byte[] data)
        {
            int sizeInBlocks = BlocksNeededToFit(blockSize, data.Length);
            var block = GetFreeBlock();

            var newBlockLen = data.Length + 2;
            stream.SetLength(block * blockSize + newBlockLen);
            stream.Seek(block * blockSize, SeekOrigin.Begin);

            stream.Write(data, 0, data.Length);
            stream.WriteByte(markerBlockEnd);
            stream.WriteByte(markerBlockEnd);

            SetFreeBlock(block + sizeInBlocks);
            return block;
        }

        private int GetBlockContentSize(int block)
        {
            var position = stream.Position;
            GotoBlock(block);
            int length = 0;
            byte prevByte = 0;
            while (true)
            {
                length++;
                int readed = stream.ReadByte();
                if ((byte)readed == markerBlockEnd && prevByte == markerBlockEnd) break;
                prevByte = (byte)readed;
                if (readed == -1) throw new InvalidDataException("Block doest not contain end marker!");
            }
            length -= 2; // Remove last two bytes of marker
            stream.Position = position;
            return length;
        }

        private void GotoBlock(int index)
        {
            var offset = blockSize * index;
            stream.Seek(offset, SeekOrigin.Begin);
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

        private static int BlocksNeededToFit(int blockSize, int length)
        {
            int fullBlocks = length / blockSize;
            int mod = length % blockSize;
            if (mod > 0) fullBlocks += 1;
            return fullBlocks;
        }

        private static int LeftSizeInBlock(int blockSize, int length)
        {
            int blocksBytes = blockSize * BlocksNeededToFit(blockSize, length);
            return blocksBytes - length - 2; // end marker length includes too
        }
    }
}
