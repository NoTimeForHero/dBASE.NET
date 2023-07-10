namespace dBASE.NET.Encoders
{
    using System.Text;

    internal class LogicalEncoder : IEncoder
    {
        /// <inheritdoc />
        public byte[] Encode(DbfField field, object data, Encoding encoding, MemoContext memo)
        {
            // Convert boolean value to string.
            string text = "?";
            if (data != null)
            {
                text = (bool)data == true ? "Y" : "N";
            }

            // Grow string to fill field length.
            text = text.PadLeft(field.Length, ' ');

            // Convert string to byte array.
            return encoding.GetBytes(text);
        }

        /// <inheritdoc />
        public object Decode(byte[] buffer, Encoding encoding, MemoContext memo)
        {
            string text = encoding.GetString(buffer).Trim().ToUpper();
            if (text == "?") return null;
            return (text == "Y" || text == "T");
        }
    }
}