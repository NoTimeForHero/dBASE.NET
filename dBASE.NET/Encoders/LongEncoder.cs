namespace dBASE.NET.Encoders
{
    using System;
    using System.Text;

    public class LongEncoder : IEncoder
    {
        private static LongEncoder instance;

        private LongEncoder() { }

        public static LongEncoder Instance => instance ?? (instance = new LongEncoder());

        /// <inheritdoc />
        public byte[] Encode(DbfField field, object data, Encoding encoding)
        {
            int value = 0;
            if (data != null) value = (int)data;
            return BitConverter.GetBytes(value);
        }

        /// <inheritdoc />
        public object Decode(byte[] buffer, byte[] memoData, Encoding encoding)
        {
            return BitConverter.ToInt64(buffer, 0);
        }
    }
}