namespace dBASE.NET.Encoders
{
    using System;
    using System.Text;

    public class LongEncoder : IEncoder
    {
        /// <inheritdoc />
        public byte[] Encode(EncoderContext context, object data)
        {
            long value = 0;
            if (data != null) value = (long)data;
            return BitConverter.GetBytes(value);
        }

        /// <inheritdoc />
        public object Decode(EncoderContext context, byte[] buffer)
        {
            return BitConverter.ToInt64(buffer, 0);
        }
    }
}