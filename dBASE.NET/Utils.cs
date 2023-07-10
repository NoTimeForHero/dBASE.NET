using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dBASE.NET
{
    internal class Utils
    {
        public static void EnsureStreamSeekable(Stream baseStream)
        {
            if (baseStream == null)
            {
                throw new ArgumentNullException(nameof(baseStream));
            }
            if (!baseStream.CanSeek)
            {
                throw new InvalidOperationException("The stream must provide positioning (support Seek method).");
            }

            baseStream.Seek(0, SeekOrigin.Begin);
        }

        public class Read
        {
            public static DbfHeader Header(BinaryReader reader)
            {
                // Peek at version number, then try to read correct version header.
                byte versionByte = reader.ReadByte();
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                DbfVersion version = (DbfVersion)versionByte;
                var newHeader = DbfHeader.CreateHeader(version);
                newHeader.Read(reader);
                return newHeader;
            }

            public static List<DbfField> Fields(BinaryReader reader, Encoding encoding)
            {
                var fields = new List<DbfField>();

                // Fields are terminated by 0x0d char.
                while (reader.PeekChar() != 0x0d)
                {
                    fields.Add(new DbfField(reader, encoding));
                }

                // Read fields terminator.
                reader.ReadByte();
                return fields;
            }

            public static byte[] Memos(Stream stream)
            {
                if (stream == null)
                {
                    throw new ArgumentNullException(nameof(stream));
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }

        public class Write
        {
            public static void Fields(BinaryWriter writer, IEnumerable<DbfField> fields, Encoding encoding)
            {
                foreach (var field in fields)
                {
                    field.Write(writer, encoding);
                }

                // Write field descriptor array terminator.
                writer.Write((byte)0x0d);
            }

            public static void Data(BinaryWriter writer, IEnumerable<DbfRecord> records, Encoding encoding, bool packRecords = false)
            {
                foreach (DbfRecord record in records)
                {
                    if (packRecords && record.IsDeleted) continue;
                    record.Write(writer, encoding);
                }

                // Write EOF character.
                writer.Write((byte)0x1a);
            }
        }

    }
}
