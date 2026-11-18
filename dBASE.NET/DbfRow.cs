namespace dBASE.NET
{
    /// <summary>A physical DBF row with the values of the requested columns.</summary>
    public readonly struct DbfRow
    {
        /// <summary>The zero-based physical record index.</summary>
        public int RowIndex { get; }

        /// <summary>Whether the record is marked for deletion.</summary>
        public bool IsDeleted { get; }

        /// <summary>The requested values in column selection order.</summary>
        public object[] Values { get; }

        internal DbfRow(int rowIndex, bool isDeleted, object[] values)
        {
            RowIndex = rowIndex;
            IsDeleted = isDeleted;
            Values = values;
        }
    }
}
