using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dBASE.NET
{
    /// <summary>
    /// This abstract classs for helping read base information
    /// </summary>
    public abstract class BaseDbf
    {
        /// <summary>
        /// Header of the file
        /// </summary>
        protected DbfHeader header;

        /// <summary>
        /// The <see cref="System.Text.Encoding" /> class that corresponds to the specified code page.
        /// Default value is <see cref="System.Text.Encoding.ASCII" />
        /// </summary>
        public Encoding Encoding { get; protected set; } = Encoding.ASCII;

        /// <summary>
        /// The collection of <see cref="DbfField" /> that represent table header.
        /// </summary>
        public IReadOnlyList<DbfField> Fields => _fields.AsReadOnly();

        /// <summary>
        /// Real collection of <see cref="DbfField" /> that represent table header.
        /// </summary>
        protected List<DbfField> _fields;

        /// <summary>
        /// Initializes a new instance of the <see cref="Dbf" />.
        /// </summary>
        public BaseDbf(Encoding encoding = null, IEnumerable<DbfField> fields = null)
        {
            header = DbfHeader.CreateHeader(DbfVersion.FoxBaseDBase3NoMemo);
            Encoding = encoding ?? Encoding;
            _fields = fields?.ToList() ?? new();
        }

        protected DbfHeader ReadHeader(BinaryReader reader)
        {
            // Peek at version number, then try to read correct version header.
            byte versionByte = reader.ReadByte();
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            DbfVersion version = (DbfVersion)versionByte;
            var newHeader = DbfHeader.CreateHeader(version);
            newHeader.Read(reader);
            return newHeader;
        }

        protected List<DbfField> ReadFields(BinaryReader reader)
        {
            var fields = new List<DbfField>();

            // Fields are terminated by 0x0d char.
            while (reader.PeekChar() != 0x0d)
            {
                fields.Add(new DbfField(reader, Encoding));
            }

            // Read fields terminator.
            reader.ReadByte();
            return fields;
        }

        protected static byte[] ReadMemos(Stream stream)
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
}
