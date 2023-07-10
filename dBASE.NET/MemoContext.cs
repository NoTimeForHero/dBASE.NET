using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dBASE.NET
{
    public class MemoContext : IDisposable
    {
        private readonly Stream stream;
        private readonly DbfHeader header;

        internal MemoContext(Stream stream, DbfHeader header)
        {
            this.stream = stream;
            this.header = header;
        }

        // TODO: Read header only on initialization
        public object GetBlockData(int index, Encoding encoding)
        {
            if (stream == null) return null;

            switch (header.Version)
            {
                case DbfVersion.dBase4WithMemo:
                case DbfVersion.FoxBaseDBase3WithMemo:
                    return Dbase(index, encoding);
                case DbfVersion.FoxPro2WithMemo:
                case DbfVersion.VisualFoxPro:
                    return FoxPro(index, encoding);
                default:
                    throw new NotImplementedException($"DBase type not supported: {header.Version}");
            }
        }

        private string Dbase(int index, Encoding encoding)
        {
            // TODO: 1. Rewrite with Stream
            // TODO: 2. Support for block size not only 512 bytes
            // TODO: 3. Support for multi block size
            // TODO: 4. Support for binary data

            const byte markerBlockEnd = 0x1A; // 0x1A/26 - block end marker

            const int blockSize = 512;
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

        private string FoxPro(int index, Encoding encoding)
        {
            // Header

            stream.Seek(0, SeekOrigin.Begin);
            int readed;

            var buffer = new byte[512];
            readed = stream.Read(buffer, 0, buffer.Length);
            if (readed != buffer.Length) 
                throw new InvalidOperationException($"Readed {readed} bytes from buffer instead {buffer.Length}");
            var blockSize = BitConverter.ToUInt16(new[] { buffer[7], buffer[6] }, 0);


            // Block
            var offset = index * blockSize;
            stream.Seek(offset, SeekOrigin.Begin);

            buffer = new byte[blockSize];
            var _ = stream.Read(buffer, 0, buffer.Length);

            int length = (int)BitConverter.ToUInt32(
                new[] { buffer[4 + 3], buffer[4 + 2], buffer[4 + 1], buffer[4 + 0] }, 0);

            offset += 8;
            stream.Seek(offset, SeekOrigin.Begin);

            buffer = new byte[length];
            readed = stream.Read(buffer, 0, buffer.Length);
            if (readed != buffer.Length) 
                throw new InvalidOperationException($"Readed {readed} bytes from buffer instead {buffer.Length}");
            return encoding.GetString(buffer).Trim();
        }

        public void Dispose() { }
    }
}
