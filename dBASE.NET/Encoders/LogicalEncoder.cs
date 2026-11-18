namespace dBASE.NET.Encoders
{
    using System;
    using System.Text;

    internal class LogicalEncoder : IEncoder
    {
        /// <inheritdoc />
        public byte[] Encode(EncoderContext context, object data)
        {
            // Convert boolean value to string.
            string text = "?";
            if (data != null)
            {
                text = (bool)data == true ? "Y" : "N";
            }

            // Grow string to fill field length.
            text = text.PadLeft(context.Field.Length, ' ');

            // Convert string to byte array.
            return context.Encoding.GetBytes(text);
        }

        /// <inheritdoc />
        public object Decode(EncoderContext context, ArraySegment<byte> field)
        {
            if (context.CanDecodeAsciiFields && field.Count == 1)
            {
                switch (field.Array[field.Offset])
                {
                    case (byte)'Y':
                    case (byte)'y':
                    case (byte)'T':
                    case (byte)'t':
                        return true;
                    case (byte)'N':
                    case (byte)'n':
                    case (byte)'F':
                    case (byte)'f':
                    case (byte)' ':
                        return false;
                    case (byte)'?':
                        return null;
                }
            }

            string text = context.Encoding.GetString(field.Array, field.Offset, field.Count).Trim().ToUpper();
            if (text == "?") return null;
            return (text == "Y" || text == "T");
        }
    }
}
