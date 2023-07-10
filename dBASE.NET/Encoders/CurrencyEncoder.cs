namespace dBASE.NET.Encoders
{
    using System;
    using System.Text;

    internal class CurrencyEncoder : IEncoder
    {
        /// <inheritdoc />
        public byte[] Encode(EncoderContext context, object data)
        {
            float value = 0;
            if (data != null) value = (float)data;
            return BitConverter.GetBytes(value);
        }

        /// <inheritdoc />
        public object Decode(EncoderContext context, byte[] buffer)
        {
            return BitConverter.ToSingle(buffer, 0);
        }
    }
}