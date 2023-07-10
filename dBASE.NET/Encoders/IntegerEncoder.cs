namespace dBASE.NET.Encoders
{
    using System;
    using System.Text;

    internal class IntegerEncoder : IEncoder
    {
        /// <inheritdoc />
        public byte[] Encode(DbfField field, object data, Encoding encoding, MemoContext memo)
        {
            int value = 0;
            if (data != null) value = (int)data;
            return BitConverter.GetBytes(value);
        }

        /// <inheritdoc />
        public object Decode(byte[] buffer, Encoding encoding, MemoContext memo)
        {
            return BitConverter.ToInt32(buffer, 0);
        }
    }
}