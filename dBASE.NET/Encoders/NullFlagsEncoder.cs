namespace dBASE.NET.Encoders
{
    using System.Text;

    internal class NullFlagsEncoder : IEncoder
    {
        /// <inheritdoc />
        public byte[] Encode(DbfField field, object data, Encoding encoding, MemoContext memo)
        {
            byte[] buffer = new byte[1];
            buffer[0] = 0;
            return buffer;
        }

        /// <inheritdoc />
        public object Decode(byte[] buffer, Encoding encoding, MemoContext memo)
        {
            return buffer[0];
        }
    }
}