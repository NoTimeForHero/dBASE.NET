namespace dBASE.NET.Encoders
{
    using System;
    using System.Text;

    internal class CurrencyEncoder : IEncoder
    {
        /// <inheritdoc />
        public byte[] Encode(DbfField field, object data, Encoding encoding, MemoContext memo)
        {
            float value = 0;
            if (data != null) value = (float)data;
            return BitConverter.GetBytes(value);
        }

        /// <inheritdoc />
        public object Decode(byte[] buffer, Encoding encoding, MemoContext memo)
        {
            return BitConverter.ToSingle(buffer, 0);
        }
    }
}