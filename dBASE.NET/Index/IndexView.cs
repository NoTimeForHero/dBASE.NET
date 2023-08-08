using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dBASE.NET.Index.Data;
using dBASE.NET.Other.Extensions;

namespace dBASE.NET.Index
{
    public class IndexView : IDisposable
    {
        private readonly Stream stream;
        private readonly BinaryReader reader;

        // Outdated:
        // https://www.clicketyclick.dk/databases/xbase/format/cdx.html#CDX_STRUCT
        // Actual:
        // https://github.com/harbour/core/blob/master/include/hbrddcdx.h

        public IndexView(Stream stream)
        {
            this.stream = stream;
            reader = new BinaryReader(stream, Encoding.ASCII);

            // 0-3 Pointer to root node
            var pointerRoot = reader.ReadInt32();
            // 4-7 Pointer to free list (-1 if empty)
            var pointerFree = reader.ReadInt32();
            // 8-11 Version no
            var version = reader.ReadInt32();
            // 12-13 Key length
            var keyLength = reader.ReadInt16();
            // 14 Index options
            var bits = reader.ReadByte();
            // 15 Index signature
            var signature = reader.ReadByte();
            stream.Position = 502;
            var sortOrder = reader.ReadInt16();
            // 504-505 TOTAL expression length (FoxPro 2)
            var totalExpLength = reader.ReadInt16();
            // 506-507 FOR expression length (binary)
            var forExpLength = reader.ReadInt16();
            // 508-509 UNUSED (Reserved)
            stream.Position += 2;
            // 510-511 Key expression length
            var keyExpLength = reader.ReadInt16();

            stream.Position = 0;
            var header = reader.ReadStructure<HbHeader>();

            stream.Position = 1024;
            ReadPage();
            ReadPage();
            ReadPage();

            Console.WriteLine("SOMETHING!");

        }

        private void ReadPage()
        {
            var position = stream.Position;
            var attributes = reader.ReadInt16();
            var keys = reader.ReadInt16();
            var leftNode = reader.ReadInt32();
            var rightNode = reader.ReadInt32();
            Console.WriteLine("PAGE!");
            stream.Position = position + 512;
        }

        public void Dispose()
        {
            reader.Dispose();
        }
    }
}
