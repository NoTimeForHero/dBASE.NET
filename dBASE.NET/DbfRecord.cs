namespace dBASE.NET
{
    using dBASE.NET.Encoders;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// DbfRecord encapsulates a record in a .dbf file. It contains an array with
    /// data (as an Object) for each field.
    /// </summary>
    public class DbfRecord
    {
        private const string defaultSeparator = ",";
        private const string defaultMask = "{name}={value}";

        private enum Marker
        {
            Normal = 0x20,
            Deleted = 0x2a,
        }

        private List<DbfField> fields;
        private MemoContext memoData;

        internal DbfRecord(BinaryReader reader, DbfHeader header, List<DbfField> fields, MemoContext memoData, Encoding encoding)
        {
            this.fields = fields;
            this.memoData = memoData;
            Data = new List<object>();

            // Read record marker.
            byte marker = reader.ReadByte();
            switch (marker)
            {
                case (byte)Marker.Normal:
                    // Normal, not-deleted
                    break;
                case (byte)Marker.Deleted:
                    IsDeleted = true;
                    break;
                default:
                    throw new ArgumentException($"Invalid marker: {marker}");
            }

            // Read entire record as sequence of bytes.
            // Note that record length includes marker.
            byte[] row = reader.ReadBytes(header.RecordLength - 1);
            if (row.Length == 0)
                throw new EndOfStreamException();

            // Read data for each field.
            int offset = 0;
            foreach (DbfField field in fields)
            {
                // Copy bytes from record buffer into field buffer.
                byte[] buffer = new byte[field.Length];
                Array.Copy(row, offset, buffer, 0, field.Length);
                offset += field.Length;

                IEncoder encoder = EncoderFactory.GetEncoder(field.Type);
                Data.Add(encoder.Decode(buffer, encoding, memoData));
            }
        }

        /// <summary>
        /// Create an empty record.
        /// </summary>
        internal DbfRecord(List<DbfField> fields)
        {
            this.fields = fields;
            Data = new List<object>();
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
            // Write marker
            var marker = (byte)(IsDeleted ? Marker.Deleted : Marker.Normal);
            writer.Write(marker);

            int index = 0;
            foreach (DbfField field in fields)
            {
                IEncoder encoder = EncoderFactory.GetEncoder(field.Type);
                byte[] buffer = encoder.Encode(field, Data[index], encoding, memoData);
                if (buffer.Length > field.Length)
                    throw new ArgumentOutOfRangeException(nameof(buffer.Length), buffer.Length, "Buffer length has exceeded length of the field.");

                writer.Write(buffer);
                index++;
            }
        }
    }
}
