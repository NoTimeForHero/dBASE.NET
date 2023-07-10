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
        }

        public string GetBlockData(int index, Encoding encoding)
        {
            var offset = blockSize * index;

            stream.Seek(offset, SeekOrigin.Begin);

            byte[] tempBlock = new byte[blockSize];
            var length = stream.Read(tempBlock, 0, blockSize);

            for (var i = 0; i < length - 1; i++)
            {
                if (tempBlock[i] != markerBlockEnd) continue;
                if (tempBlock[i + 1] == markerBlockEnd)
                {
                    length = i;
                    break;
                }
            }

            var data = new byte[length];
            Buffer.BlockCopy(tempBlock, 0, data, 0, length);
            return encoding.GetString(data).Trim();
        }

        public void WriteBlockData(int index, byte[] data)
        {
            /*
            if (data.Length > blockSize) throw new NotImplementedException("Multiblock not supported now!");

            var offset = blockSize * index;
            stream.Seek(offset, SeekOrigin.Begin);

            stream.Write(data, 0, data.Length);
            stream.WriteByte(markerBlockEnd);
            stream.WriteByte(markerBlockEnd);
            */
        }
    }
}
