using System;
using System.IO;
using System.Text;

namespace dBASE.NET.Memo.Adapters
{
    internal class FoxProAdapter : IMemoAdapter
    {
        private Stream stream;
        private int blockSize;

        public void Initialize(Stream stream)
        {
            this.stream = stream;

            stream.Seek(0, SeekOrigin.Begin);
            int readed;

            var buffer = new byte[512];
            readed = stream.Read(buffer, 0, buffer.Length);
            if (readed != buffer.Length)
                throw new InvalidOperationException($"Readed {readed} bytes from buffer instead {buffer.Length}");
            blockSize = BitConverter.ToUInt16(new[] { buffer[7], buffer[6] }, 0);
        }

        public string GetBlockData(int index, Encoding encoding)
        {
            // Block
            var offset = index * blockSize;
            stream.Seek(offset, SeekOrigin.Begin);

            var buffer = new byte[blockSize];
            var _ = stream.Read(buffer, 0, buffer.Length);

            int length = (int)BitConverter.ToUInt32(
                new[] { buffer[4 + 3], buffer[4 + 2], buffer[4 + 1], buffer[4 + 0] }, 0);

            offset += 8;
            stream.Seek(offset, SeekOrigin.Begin);

            buffer = new byte[length];
            int readed = stream.Read(buffer, 0, buffer.Length);
            if (readed != buffer.Length)
                throw new InvalidOperationException($"Readed {readed} bytes from buffer instead {buffer.Length}");
            return encoding.GetString(buffer).Trim();
        }

        public void WriteBlockData(int index, byte[] data)
        {
            throw new NotImplementedException();
        }

        public int AppendBlock(byte[] data)
        {
            throw new NotImplementedException();
        }
    }
}
