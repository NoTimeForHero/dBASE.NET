using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dBASE.NET.Encoders;

namespace dBASE.NET
{
    /// <summary>
    /// Class for fast working with existing DBF files.
    /// </summary>
    public class FastDbf : BaseDbf, IDisposable
    {
        private readonly bool readOnly;
        private readonly Stream baseStream;
        private readonly BinaryReader reader;
        private readonly BinaryWriter writer;
        private EncoderContext encoderContext;
        private int[] fieldOffsets;

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
        public FastDbf(Stream baseStream, Stream memoStream = null, Encoding encoding = null, bool readOnly = false) : base(encoding)
        {
            Utils.EnsureStreamSeekable(baseStream);
            this.readOnly = readOnly;
            this.baseStream = baseStream;
            reader = new BinaryReader(baseStream, Encoding.ASCII);
            if (!readOnly) writer = new BinaryWriter(baseStream, Encoding.ASCII);
            Initialize(memoStream);
        }

        private void Initialize(Stream memoStream)
        {
            header = Utils.Read.Header(reader);
            _fields = Utils.Read.Fields(reader, Encoding);

            var offset = 0;
            fieldOffsets = new int[_fields.Count];
            for (var i=0; i<_fields.Count; i++ )
            {
                fieldOffsets[i] = offset;
                offset += _fields[i].Length;
            }

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
            encoderContext = new EncoderContext { Encoding = Encoding, Memo = memo };
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

        private int CalculateOffset(int row, int column)
        {
            if (row >= RecordCount) throw new ArgumentOutOfRangeException(nameof(row));
            if (column >= _fields.Count) throw new ArgumentOutOfRangeException(nameof(column));
            var deletedMarkerLength = 1;
            var fieldOffset = fieldOffsets[column];
            var offset = header.HeaderLength + row * header.RecordLength + deletedMarkerLength + fieldOffset;
            return offset;
        }

        /// <summary>
        /// Get column value without reading full <see cref="DbfRecord"/>
        /// <para>Usefull for DBF with large amount of columns (50 and more)</para>
        /// </summary>
        public object GetValue(int row, int column)
        {
            var offset = CalculateOffset(row, column);
            baseStream.Seek(offset, SeekOrigin.Begin);
            var field = _fields[column];
            encoderContext.Field = field;
            var buffer = reader.ReadBytes(field.Length);
            IEncoder encoder = EncoderFactory.GetEncoder(field.Type);
            return encoder.Decode(encoderContext, buffer);
        }

        /// <summary>
        /// Set column value without reading full <see cref="DbfRecord"/>
        /// <para>Usefull for DBF with large amount of columns (50 and more)</para>
        /// </summary>
        public void SetValue(int row, int column, object value)
        {
            if (readOnly) throw new InvalidOperationException("File is Readonly!");
            // BUG: With MEMO field this method ALWAYS create new block!
            var offset = CalculateOffset(row, column);
            var field = _fields[column];
            IEncoder encoder = EncoderFactory.GetEncoder(field.Type);
            encoderContext.Field = field;
            byte[] buffer = encoder.Encode(encoderContext, value);
            if (buffer.Length > field.Length)
                throw new ArgumentOutOfRangeException(
                    nameof(buffer.Length), buffer.Length, "Buffer length has exceeded length of the field.");
            baseStream.Seek(offset, SeekOrigin.Begin);
            writer.Write(buffer);
        }

        /// <summary>
        /// Writes record to file by offset<br/>
        /// For add new record use special method <see cref="AppendRecord(DbfRecord)"/>
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void WriteRecord(int index, DbfRecord record)
        {
            if (readOnly) throw new InvalidOperationException("File is Readonly!");
            if (index > RecordCount - 1) throw new ArgumentOutOfRangeException(nameof(index));
            var offset = header.HeaderLength + index * header.RecordLength;
            baseStream.Seek(offset, SeekOrigin.Begin);
            record.Write(writer, Encoding);
        }

        /// <summary>
        /// Creates a new <see cref="DbfRecord" /> with the same schema as the table.
        /// </summary>
        /// <returns>A <see cref="DbfRecord" /> with the same schema as the <see cref="T:System.Data.DataTable" />.</returns>
        public DbfRecord CreateRecord()
        {
            if (readOnly) throw new InvalidOperationException("File is Readonly!");
            return new(_fields, memo, Encoding);
        }

        /// <summary>
        /// Writes new record to the end of the file
        /// </summary>
        /// <param name="record">Record to write</param>
        public void AppendRecord(DbfRecord record)
        {
            if (readOnly) throw new InvalidOperationException("File is Readonly!");
            baseStream.Seek(-1, SeekOrigin.End); // Skip end of file marker
            record.Write(writer, Encoding);
            writer.Write((byte)0x1a);
            RecordCount += 1;
            header.SetRecordCount(writer, RecordCount);
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
