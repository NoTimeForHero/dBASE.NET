using System.Collections.ObjectModel;

namespace dBASE.NET
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    /// <summary>
    /// The Dbf class encapsulated a dBASE table (.dbf) file, allowing
    /// reading from disk, writing to disk, enumerating fields and enumerating records.
    /// [WARNING] This class reads ands writes ALL file to the memory! Can be slow on large DBF.
    /// </summary>
    public class Dbf : BaseDbf
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Dbf" />.
        /// </summary>
        public Dbf(Encoding encoding = null, IEnumerable<DbfField> fields = null) : base(encoding, fields) {}

        /// <summary>
        /// <para>Readonly collection of <see cref="DbfRecord" /> that contains table data.</para>
        /// For add new record view <see cref="CreateRecord"/>.<br/>
        /// To delete data update <see cref="DbfRecord.IsDeleted"/> property and overwrite dbf with PACK mode.
        /// </summary>
        public IReadOnlyList<DbfRecord> Records => _records.AsReadOnly();
        private List<DbfRecord> _records = new();

        /// <summary>
        /// Main header of type <see cref="DbfHeader" />
        /// </summary>
        public DbfHeader Header => header;

        /// <summary>
        /// Creates a new <see cref="DbfRecord" /> with the same schema as the table.
        /// </summary>
        /// <returns>A <see cref="DbfRecord" /> with the same schema as the <see cref="T:System.Data.DataTable" />.</returns>
        public DbfRecord CreateRecord()
        {
            DbfRecord record = new DbfRecord(_fields);
            _records.Add(record);
            return record;
        }

        /// <summary>
        /// Creates a new file, writes the current instance to the file, and then closes the file. If the target file already exists, it is overwritten.
        /// </summary>
        /// <param name="stream">The output stream.</param>
        /// <param name="version">The version <see cref="DbfVersion" />. If unknown specified, use current header version.</param>
        /// <param name="packRecords">Remove all records that were marked as deleted</param>
        /// <param name="leaveOpen">Keep the BinaryWriter open</param>
        public void Write(Stream stream, DbfVersion version = DbfVersion.Unknown, bool packRecords = false, bool leaveOpen = false)
        {
            if (version != DbfVersion.Unknown)
            {
                header.Version = version;
                header = DbfHeader.CreateHeader(header.Version);
            }

            using (BinaryWriter writer = new BinaryWriter(stream, Encoding, leaveOpen))
            {
                header.Write(writer, _fields, _records);
                WriteFields(writer);
                WriteData(writer, packRecords);
            }
        }

        private void WriteFields(BinaryWriter writer)
        {
            foreach (DbfField field in Fields)
            {
                field.Write(writer, Encoding);
            }

            // Write field descriptor array terminator.
            writer.Write((byte)0x0d);
        }

        private void WriteData(BinaryWriter writer, bool packRecords = false)
        {
            foreach (DbfRecord record in _records)
            {
                if (packRecords && record.IsDeleted) continue;
                record.Write(writer, Encoding);
            }

            // Write EOF character.
            writer.Write((byte)0x1a);
        }

        private List<DbfRecord> ReadRecords(BinaryReader reader, byte[] memoData)
        {
            var records = new List<DbfRecord>();
            // Records are terminated by 0x1a char (officially), or EOF (also seen).
            while (reader.PeekChar() != 0x1a && reader.PeekChar() != -1)
            {
                try
                {
                    records.Add(new DbfRecord(reader, header, _fields, memoData, Encoding));
                }
                catch (EndOfStreamException) { }
            }
            return records;
        }

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
                header = ReadHeader(reader);
                byte[] memoData = memoStream != null ? ReadMemos(memoStream) : null;
                _fields = ReadFields(reader);

                // After reading the fields, we move the read pointer to the beginning
                // of the records, as indicated by the "HeaderLength" value in the header.
                baseStream.Seek(header.HeaderLength, SeekOrigin.Begin);
                _records = ReadRecords(reader, memoData);
            }
        }


    }
}
