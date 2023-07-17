using System;
using System.IO;
using System.Text;

namespace dBASE.NET.Memo.Adapters
{
    internal class DbaseAdapter : IMemoAdapter
    {
        private Stream stream;
        private int blockSize = 512;
        const byte markerBlockEnd = 0x1A; // 0x1A/26 - block end marker

        public void Initialize(Stream stream)
        {
            this.stream = stream;
            var len = stream.Length;

            if (len > 0 && len < blockSize) throw new InvalidDataException("Corrupted header!");
            if (len == 0) GenerateHeader();
        }

        public string GetBlockData(int index, Encoding encoding)
        {
            var offset = blockSize * index;

            stream.Seek(offset, SeekOrigin.Begin);

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

            var data = new byte[length];
            stream.Seek(offset, SeekOrigin.Begin);
            var _ = stream.Read(data, 0, data.Length);
            return encoding.GetString(data).Trim();
        }

        public void WriteBlockData(int index, byte[] data)
        {
            if (data.Length > blockSize) throw new NotImplementedException("Multiblock not supported now!");

            var offset = blockSize * index;
            stream.Seek(offset, SeekOrigin.Begin);

            stream.Write(data, 0, data.Length);
            stream.WriteByte(markerBlockEnd);
            stream.WriteByte(markerBlockEnd);
        }

        public int AppendBlock(byte[] data)
        {
            if (data.Length > blockSize) throw new NotImplementedException("Multiblock not supported now!");

            var block = GetFreeBlock();

            var newBlockLen = data.Length + 2;
            stream.SetLength(block * blockSize + newBlockLen);
            stream.Seek(block * blockSize, SeekOrigin.Begin);

            stream.Write(data, 0, data.Length);
            stream.WriteByte(markerBlockEnd);
            stream.WriteByte(markerBlockEnd);

            SetFreeBlock(block + 1);
            return block;
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
            stream.Seek(0, SeekOrigin.Begin);
            var buffer = new byte[4];
            var _ = stream.Read(buffer, 0, buffer.Length);
            var number = BitConverter.ToInt32(buffer, 0);
            return number;
        }

        private void SetFreeBlock(int value)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var buffer = BitConverter.GetBytes(value);
            stream.Write(buffer, 0, buffer.Length);
        }
    }
}
