using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dBASE.NET
{
    /// <summary>
    /// Class for fast working with existing DBF files.
    /// </summary>
    public class FastDbf : BaseDbf, IDisposable
    {
        private readonly Stream baseStream;
        private readonly BinaryReader reader;
        private readonly BinaryWriter writer;

        /// <summary>
        /// Total count of records in file
        /// </summary>
        public int RecordCount { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FastDbf" />.
        /// </summary>
        public static FastDbf ReadFile(string path, Encoding encoding = null)
        {
            // Open stream for reading.
            var baseStream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            string memoPath = Utils.GetMemoPath(path);
            if (memoPath == null)
            {
                return new FastDbf(baseStream, encoding: encoding);
            }
            var memoStream = File.Open(memoPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            return new FastDbf(baseStream, memoStream, encoding);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FastDbf" />.
        /// </summary>
        /// <exception cref="InvalidOperationException">If you trying to load file with MEMO fields without MEMO stream</exception>
        public FastDbf(Stream baseStream, Stream memoStream = null, Encoding encoding = null) : base(encoding)
        {
            Utils.EnsureStreamSeekable(baseStream);
            reader = new BinaryReader(baseStream, Encoding.ASCII);
            writer = new BinaryWriter(baseStream, Encoding.ASCII);
            this.baseStream = baseStream;
            Initialize(memoStream);
        }

        private void Initialize(Stream memoStream)
        {
            header = Utils.Read.Header(reader);
            _fields = Utils.Read.Fields(reader, Encoding);

            if (memoStream == null)
            {
                var hasMemoField = _fields.Any(x => x.Type == DbfFieldType.Memo);
                if (hasMemoField) throw new InvalidOperationException("Missed MEMO file for DBF file with MEMO fields!");
            }
            else
            {
                memo.Initialize(memoStream, header.Version);
            }

            // After reading the fields, we move the read pointer to the beginning
            // of the records, as indicated by the "HeaderLength" value in the header.
            baseStream.Seek(header.HeaderLength, SeekOrigin.Begin);

            var bodyLength = baseStream.Length - header.HeaderLength;
            RecordCount = (int)(bodyLength / header.RecordLength);
        }

        /// <summary>
        /// Get a record by given index
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public DbfRecord GetRecord(int index)
        {
            if (index > RecordCount - 1) throw new ArgumentOutOfRangeException(nameof(index));
            var offset = header.HeaderLength + index * header.RecordLength;
            baseStream.Seek(offset, SeekOrigin.Begin);
            return new DbfRecord(reader, header, _fields, memo, Encoding);
        }

        /// <summary>
        /// Writes record to file by offset<br/>
        /// For add new record use special method <see cref="AppendRecord(DbfRecord)"/>
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void WriteRecord(int index, DbfRecord record)
        {
            if (index > RecordCount - 1) throw new ArgumentOutOfRangeException(nameof(index));
            var offset = header.HeaderLength + index * header.RecordLength;
            baseStream.Seek(offset, SeekOrigin.Begin);
            record.Write(writer, Encoding);
        }

        /// <summary>
        /// Creates a new <see cref="DbfRecord" /> with the same schema as the table.
        /// </summary>
        /// <returns>A <see cref="DbfRecord" /> with the same schema as the <see cref="T:System.Data.DataTable" />.</returns>
        public DbfRecord CreateRecord() => new(_fields, memo, Encoding);

        /// <summary>
        /// Writes new record to the end of the file
        /// </summary>
        /// <param name="record">Record to write</param>
        public void AppendRecord(DbfRecord record)
        {
            baseStream.Seek(0, SeekOrigin.End);
            record.Write(writer, Encoding);
        }

        /// <summary>
        /// Closes all child streams
        /// </summary>
        public void Dispose()
        {
            baseStream?.Dispose();
            memo?.Dispose();
            reader?.Dispose();
            writer?.Dispose();
        }
    }
}
