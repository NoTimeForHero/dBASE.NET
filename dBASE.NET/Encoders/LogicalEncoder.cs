namespace dBASE.NET.Encoders
{
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
        public object Decode(EncoderContext context, byte[] buffer)
        {
            string text = context.Encoding.GetString(buffer).Trim().ToUpper();
            if (text == "?") return null;
            return (text == "Y" || text == "T");
        }
    }
}