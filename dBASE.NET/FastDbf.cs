using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dBASE.NET.Encoders;
using dBASE.NET.Memo;

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
        private readonly Stream unusedMemoStream;
        private EncoderContext encoderContext;
        private int[] fieldOffsets;

        /// <summary>
        /// Total count of records in file
        /// </summary>
        public int RecordCount { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FastDbf" />.
        /// </summary>
        public static FastDbf ReadFile(string path, Encoding encoding = null, bool readOnly = false)
        {
            var baseStream = File.Open(path, FileMode.Open, Access(readOnly), FileShare.ReadWrite);
            try
            {
                return new FastDbf(baseStream, null, encoding, readOnly, path);
            }
            catch
            {
                baseStream.Dispose();
                throw;
            }
        }

        private static FileAccess Access(bool readOnly) => readOnly ? FileAccess.Read : FileAccess.ReadWrite;

        /// <summary>
        /// Initializes a new instance of the <see cref="FastDbf" />.
        /// </summary>
        /// <exception cref="InvalidOperationException">The table has memo fields and no memo stream was supplied.</exception>
        public FastDbf(Stream baseStream, Stream memoStream = null, Encoding encoding = null, bool readOnly = false)
            : this(baseStream, memoStream, encoding, readOnly, null) { }

        private FastDbf(Stream baseStream, Stream memoStream, Encoding encoding, bool readOnly, string path)
            : base(encoding)
        {
            Utils.EnsureStreamSeekable(baseStream);
            this.readOnly = readOnly;
            this.baseStream = baseStream;
            reader = new BinaryReader(baseStream, Encoding.ASCII);
            if (!readOnly) writer = new BinaryWriter(baseStream, Encoding.ASCII);
            header = Utils.Read.Header(reader);
            _fields = Utils.Read.Fields(reader, Encoding);

            var offset = 0;
            fieldOffsets = new int[_fields.Count];
            for (var i=0; i<_fields.Count; i++ )
            {
                fieldOffsets[i] = offset;
                offset += _fields[i].Length;
            }

            // After reading the fields, we move the read pointer to the beginning
            // of the records, as indicated by the "HeaderLength" value in the header.
            baseStream.Seek(header.HeaderLength, SeekOrigin.Begin);

            var bodyLength = baseStream.Length - header.HeaderLength;
            RecordCount = (int)(bodyLength / header.RecordLength);
            encoderContext = new EncoderContext { Encoding = Encoding, Memo = memo };

            if (_fields.Any(field => field.Type == DbfFieldType.Memo))
                InitializeMemo(memoStream, path);
            else
                unusedMemoStream = memoStream;
        }

        private void InitializeMemo(Stream memoStream, string path)
        {
            if (path != null)
            {
                string memoPath = MemoFormatParser.FromVersion(header.Version) == MemoFormat.FPT
                    ? Path.ChangeExtension(path, ".fpt")
                    : Utils.GetMemoPath(path);
                if (File.Exists(memoPath))
                    memoStream = File.Open(memoPath, FileMode.Open, Access(readOnly), FileShare.ReadWrite);
            }

            if (memoStream == null)
                throw new InvalidOperationException("Missing memo file for a DBF table with memo fields.");

            try
            {
                memo.Initialize(memoStream, header.Version, readOnly);
            }
            catch
            {
                if (path != null) memoStream.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Get a record by given index
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public DbfRecord GetRecord(int rowIndex)
        {
            if ((uint)rowIndex >= (uint)RecordCount) throw new ArgumentOutOfRangeException(nameof(rowIndex));
            baseStream.Seek(RecordOffset(rowIndex), SeekOrigin.Begin);
            return new DbfRecord(reader, header, _fields, memo, Encoding);
        }

        /// <summary>Reads full records sequentially in physical order, including deleted records.</summary>
        /// <remarks>Keep the table open and its schema unchanged while enumerating.</remarks>
        public IEnumerable<DbfRecord> ReadRecords()
        {
            DbfReadColumn[] selected = null;
            foreach (var record in DbfRecordReader.Read(baseStream, header, RecordCount))
            {
                bool isDeleted = DbfRecord.DecodeDeletionMarker(record.Array[record.Offset]);
                selected ??= DbfReadColumn.Prepare(_fields);
                yield return new DbfRecord(_fields, memo, Encoding, isDeleted, record, selected);
            }
        }

        /// <summary>Reads the requested columns sequentially in physical record order, including deleted records.</summary>
        /// <param name="columns">Zero-based column indexes in the order to return. An empty selection reads row metadata.</param>
        /// <remarks>Keep the table open and its schema unchanged while enumerating.</remarks>
        public IEnumerable<DbfRow> ReadValues(params int[] columns)
        {
            if (columns == null)
                throw new ArgumentNullException(nameof(columns));

            var selected = DbfReadColumn.Prepare(_fields, columns);
            var context = new EncoderContext { Encoding = Encoding, Memo = memo };
            int rowIndex = 0;
            foreach (var record in DbfRecordReader.Read(baseStream, header, RecordCount))
            {
                bool isDeleted = DbfRecord.DecodeDeletionMarker(record.Array[record.Offset]);
                var values = selected.Length == 0 ? Array.Empty<object>() : new object[selected.Length];
                for (int i = 0; i < selected.Length; i++)
                    values[i] = selected[i].Decode(context, record);

                yield return new DbfRow(rowIndex++, isDeleted, values);
            }
        }

        /// <summary>
        /// Check is record at specific index is deleted
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public bool IsDeleted(int rowIndex)
        {
            if ((uint)rowIndex >= (uint)RecordCount) throw new ArgumentOutOfRangeException(nameof(rowIndex));
            baseStream.Seek(RecordOffset(rowIndex), SeekOrigin.Begin);
            byte marker = reader.ReadByte();
            return marker == (byte)DbfRecordMarker.Deleted;
        }

        private long RecordOffset(int row) => header.HeaderLength + (long)row * header.RecordLength;

        private long CalculateOffset(int row, int column)
        {
            if ((uint)row >= (uint)RecordCount) throw new ArgumentOutOfRangeException(nameof(row));
            if ((uint)column >= (uint)_fields.Count) throw new ArgumentOutOfRangeException(nameof(column));
            return RecordOffset(row) + 1 + fieldOffsets[column];
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
            return encoder.Decode(encoderContext, new ArraySegment<byte>(buffer));
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
            if ((uint)index >= (uint)RecordCount) throw new ArgumentOutOfRangeException(nameof(index));
            baseStream.Seek(RecordOffset(index), SeekOrigin.Begin);
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
            unusedMemoStream?.Dispose();
            reader?.Dispose();
            writer?.Dispose();
        }
    }
}
