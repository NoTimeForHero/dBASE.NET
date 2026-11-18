namespace dBASE.NET.Encoders
{
    using System;
    using System.Text;

    internal class IntegerEncoder : IEncoder
    {
        /// <inheritdoc />
        public byte[] Encode(EncoderContext context, object data)
        {
            int value = 0;
            if (data != null) value = (int)data;
            return BitConverter.GetBytes(value);
        }

        /// <inheritdoc />
        public object Decode(EncoderContext context, ArraySegment<byte> field)
        {
            FieldBytes.RequireWidth(field, sizeof(int));
            return BitConverter.ToInt32(field.Array, field.Offset);
        }
    }
}
