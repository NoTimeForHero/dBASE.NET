using System;
using System.Collections.Generic;

namespace dBASE.NET.Encoders
{
    internal readonly struct DbfReadColumn
    {
        private readonly DbfField field;
        private readonly IEncoder decoder;
        private readonly int offset;
        private readonly int length;

        private DbfReadColumn(DbfField field, int offset, bool projection)
        {
            this.field = field;
            this.offset = offset;
            length = field.Length;
            decoder = projection && field.Type == DbfFieldType.Memo
                ? MemoEncoder.ProjectionDecoder
                : EncoderFactory.GetEncoder(field.Type);
        }

        internal static DbfReadColumn[] Prepare(IReadOnlyList<DbfField> fields, int[] columns = null, bool projection = false)
        {
            var offsets = new int[fields.Count];
            int offset = 1; // Deletion marker.
            for (int i = 0; i < fields.Count; i++)
            {
                offsets[i] = offset;
                offset += fields[i].Length;
            }

            var prepared = new DbfReadColumn[columns?.Length ?? fields.Count];
            for (int i = 0; i < prepared.Length; i++)
            {
                int ordinal = columns == null ? i : columns[i];
                if ((uint)ordinal >= (uint)fields.Count)
                    throw new ArgumentOutOfRangeException(nameof(columns), ordinal, "Column index is outside the DBF schema.");
                prepared[i] = new DbfReadColumn(fields[ordinal], offsets[ordinal], projection);
            }

            return prepared;
        }

        internal object Decode(EncoderContext context, ArraySegment<byte> record)
        {
            if (offset + length > record.Count)
                throw new ArgumentException("The field extends beyond the end of the DBF record.", nameof(record));

            context.Field = field;
            return decoder.Decode(context, new ArraySegment<byte>(record.Array, record.Offset + offset, length));
        }
    }
}
