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
        protected readonly List<DbfField> _fields;

        public BaseDbf(IEnumerable<DbfField> fields = null) => _fields = fields?.ToList() ?? new();

        /// <summary>
        /// Reads the contents of streams that initialize the current instance.
        /// </summary>
        /// <param name="baseStream">Stream with a database.</param>
        /// <param name="memoStream">Stream with a memo.</param>
        public void Read(Stream baseStream, Stream memoStream = null)
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

            //using (BinaryReader reader = new BinaryReader(baseStream))
            using (BinaryReader reader = new BinaryReader(baseStream, Encoding.ASCII))          //ReadFields() use PeekChar to detect end flag=0D, default Encoding may be UTF8 then clause exception
            {
                ReadHeader(reader);
                byte[] memoData = memoStream != null ? ReadMemos(memoStream) : null;
                ReadFields(reader);

                // After reading the fields, we move the read pointer to the beginning
                // of the records, as indicated by the "HeaderLength" value in the header.
                baseStream.Seek(header.HeaderLength, SeekOrigin.Begin);
                ReadData(reader, memoData);
            }
        }

        protected abstract void ReadData(BinaryReader reader, byte[] memoData);

        private void ReadHeader(BinaryReader reader)
        {
            // Peek at version number, then try to read correct version header.
            byte versionByte = reader.ReadByte();
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            DbfVersion version = (DbfVersion)versionByte;
            header = DbfHeader.CreateHeader(version);
            header.Read(reader);
        }

        private void ReadFields(BinaryReader reader)
        {
            _fields.Clear();

            // Fields are terminated by 0x0d char.
            while (reader.PeekChar() != 0x0d)
            {
                _fields.Add(new DbfField(reader, Encoding));
            }

            // Read fields terminator.
            reader.ReadByte();
        }

        private static byte[] ReadMemos(Stream stream)
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
