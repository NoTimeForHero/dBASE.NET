using System;
using dBASE.NET.Encoders;

namespace dBASE.NET
{
    /// <summary>Reads a row during a synchronous <see cref="FastDbf.VisitRows"/> callback.</summary>
    public delegate void DbfRowVisitor(DbfRowReader row);

    /// <summary>Provides on-demand access to the selected columns of the current physical row.</summary>
    /// <remarks>Column positions are zero-based and follow the selection passed to <see cref="FastDbf.VisitRows"/>.</remarks>
    public readonly ref struct DbfRowReader
    {
        private readonly ArraySegment<byte> record;
        private readonly DbfReadColumn[] columns;
        private readonly EncoderContext context;

        /// <summary>The zero-based physical record index.</summary>
        public int RowIndex { get; }

        /// <summary>Whether the record is marked for deletion.</summary>
        public bool IsDeleted { get; }

        internal DbfRowReader(int rowIndex, bool isDeleted, ArraySegment<byte> record,
            DbfReadColumn[] columns, EncoderContext context)
        {
            RowIndex = rowIndex;
            IsDeleted = isDeleted;
            this.record = record;
            this.columns = columns;
            this.context = context;
        }

        /// <summary>Reads a selected value using the field's standard decoder.</summary>
        public object GetValue(int column) => GetColumn(column).Decode(context, record);

        /// <summary>Reads a Numeric (N) field. Blank values return <see langword="null"/>.</summary>
        /// <exception cref="InvalidOperationException">The field is not Numeric.</exception>
        public double? GetNumber(int column) => GetColumn(column).DecodeNumber(context, record);

        /// <summary>Reads a Character (C) field. Blank values return <see langword="null"/>.</summary>
        /// <exception cref="InvalidOperationException">The field is not Character.</exception>
        public string GetText(int column) => GetColumn(column).DecodeText(context, record);

        private ref readonly DbfReadColumn GetColumn(int column)
        {
            if ((uint)column >= (uint)columns.Length)
                throw new ArgumentOutOfRangeException(nameof(column));

            return ref columns[column];
        }
    }
}
