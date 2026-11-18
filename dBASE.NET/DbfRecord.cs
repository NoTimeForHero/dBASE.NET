namespace dBASE.NET
{
    using dBASE.NET.Encoders;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    internal enum DbfRecordMarker
    {
        Normal = 0x20,
        Deleted = 0x2a,
    }

    /// <summary>
    /// DbfRecord encapsulates a record in a .dbf file. It contains an array with
    /// data (as an Object) for each field.
    /// </summary>
    public class DbfRecord
    {
        private const string defaultSeparator = ",";
        private const string defaultMask = "{name}={value}";

        private readonly List<DbfField> fields;
        private readonly EncoderContext encoderContext;

        private DbfRecord(List<DbfField> fields, MemoContext memoData, Encoding encoding, bool isDeleted)
        {
            this.fields = fields;
            Data = new List<object>(fields.Count);
            encoderContext = new EncoderContext { Encoding = encoding, Memo = memoData };
            IsDeleted = isDeleted;
        }

        internal DbfRecord(BinaryReader reader, DbfHeader header, List<DbfField> fields, MemoContext memoData, Encoding encoding,
            byte[] readBuffer = null)
            : this(fields, memoData, encoding, DecodeDeletionMarker(reader.ReadByte()))
        {
            // Read entire record as sequence of bytes.
            // Note that record length includes marker.
            int payloadLength = header.RecordLength - 1;
            byte[] row;
            int bytesRead;
#if NET8_0_OR_GREATER
            if (readBuffer != null)
            {
                row = readBuffer;
                bytesRead = reader.BaseStream.ReadAtLeast(row.AsSpan(0, payloadLength), payloadLength, throwOnEndOfStream: false);
            }
            else
#endif
            {
                row = reader.ReadBytes(payloadLength);
                bytesRead = row.Length;
            }
            if (bytesRead == 0)
                throw new EndOfStreamException();

            // Read data for each field.
            int offset = 0;
            foreach (DbfField field in fields)
            {
                encoderContext.Field = field;
#if NET8_0_OR_GREATER
                if (bytesRead != row.Length && offset + field.Length > bytesRead)
                    throw new ArgumentException("The field extends beyond the end of the DBF record.");
#endif
                var fieldBytes = new ArraySegment<byte>(row, offset, field.Length);
                IEncoder encoder = EncoderFactory.GetEncoder(field.Type);
                Data.Add(encoder.Decode(encoderContext, fieldBytes));
                offset += fieldBytes.Count;
            }
        }

        internal DbfRecord(List<DbfField> fields, MemoContext memoData, Encoding encoding,
            bool isDeleted, ArraySegment<byte> record, DbfReadColumn[] columns)
            : this(fields, memoData, encoding, isDeleted)
        {
            foreach (var column in columns)
                Data.Add(column.Decode(encoderContext, record));
        }

        internal static bool DecodeDeletionMarker(byte marker) => marker switch
        {
            (byte)DbfRecordMarker.Normal => false,
            (byte)DbfRecordMarker.Deleted => true,
            _ => throw new ArgumentException($"Invalid marker: {marker}")
        };

        /// <summary>
        /// Create an empty record.
        /// </summary>
        internal DbfRecord(List<DbfField> fields, MemoContext memoData, Encoding encoding)
            : this(fields, memoData, encoding, false)
        {
            foreach (DbfField field in fields) Data.Add(null);
        }

        public List<object> Data { get; }

        /// <summary>
        /// Check is record a delete (0x20 if not)
        /// </summary>
        public bool IsDeleted { get; set; }

        public object this[int index] => Data[index];

        public object this[string name]
        {
            get
            {
                int index = fields.FindIndex(x => x.Name.Equals(name));
                if (index == -1) return null;
                return Data[index];
            }
        }

        public object this[DbfField field]
        {
            get
            {
                int index = fields.IndexOf(field);
                if (index == -1) return null;
                return Data[index];
            }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return ToString(defaultSeparator, defaultMask);
        }

        /// <summary>
        /// Returns a string that represents the current object with custom separator.
        /// </summary>
        /// <param name="separator">Custom separator.</param>
        /// <returns>A string that represents the current object with custom separator.</returns>
        public string ToString(string separator)
        {
            return ToString(separator, defaultMask);
        }

        /// <summary>
        /// Returns a string that represents the current object with custom separator and mask.
        /// </summary>
        /// <param name="separator">Custom separator.</param>
        /// <param name="mask">
        /// Custom mask.
        /// <para>e.g., "{name}={value}", where {name} is the mask for the field name, and {value} is the mask for the value.</para>
        /// </param>
        /// <returns>A string that represents the current object with custom separator and mask.</returns>
        public string ToString(string separator, string mask)
        {
            separator = separator ?? defaultSeparator;
            mask = (mask ?? defaultMask).Replace("{name}", "{0}").Replace("{value}", "{1}");

            return string.Join(separator, fields.Select(z => string.Format(mask, z.Name, this[z])));
        }

        internal void Write(BinaryWriter writer, Encoding encoding)
        {
            encoderContext.Encoding = encoding;
            // Write marker
            var marker = (byte)(IsDeleted ? DbfRecordMarker.Deleted : DbfRecordMarker.Normal);
            writer.Write(marker);

            int index = 0;
            foreach (DbfField field in fields)
            {
                IEncoder encoder = EncoderFactory.GetEncoder(field.Type);
                encoderContext.Field = field;
                byte[] buffer = encoder.Encode(encoderContext, Data[index]);
                if (buffer.Length > field.Length)
                    throw new ArgumentOutOfRangeException(nameof(buffer.Length), buffer.Length, "Buffer length has exceeded length of the field.");

                writer.Write(buffer);
                index++;
            }
        }
    }
}
