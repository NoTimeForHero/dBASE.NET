namespace dBASE.NET.Encoders
{
    using System;

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
        public object Decode(EncoderContext context, ArraySegment<byte> field)
        {
            if (field.Count == 0) throw new IndexOutOfRangeException();
            return field.Array[field.Offset];
        }
    }
}
