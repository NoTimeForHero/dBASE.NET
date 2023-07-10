namespace dBASE.NET.Encoders
{
    using System.Text;

    internal class NullFlagsEncoder : IEncoder
    {
        /// <inheritdoc />
        public byte[] Encode(EncoderContext context, object data)
        {
            byte[] buffer = new byte[1];
            buffer[0] = 0;
            return buffer;
        }

        /// <inheritdoc />
        public object Decode(EncoderContext context, byte[] buffer)
        {
            return buffer[0];
        }
    }
}