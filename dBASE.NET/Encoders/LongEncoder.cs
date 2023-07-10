namespace dBASE.NET.Encoders
{
    using System;
    using System.Text;

    public class LongEncoder : IEncoder
    {
        /// <inheritdoc />
        public byte[] Encode(DbfField field, object data, Encoding encoding, MemoContext memo)
        {
            long value = 0;
            if (data != null) value = (long)data;
            return BitConverter.GetBytes(value);
        }

        /// <inheritdoc />
        public object Decode(byte[] buffer, Encoding encoding, MemoContext memo)
        {
            return BitConverter.ToInt64(buffer, 0);
        }
    }
}