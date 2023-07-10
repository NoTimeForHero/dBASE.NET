namespace dBASE.NET.Encoders
{
    using System;
    using System.Globalization;
    using System.Text;

    internal class FloatEncoder : IEncoder
    {
        /// <inheritdoc />
        public byte[] Encode(EncoderContext context, object data)
        {
            var field = context.Field;
            string text = Convert.ToString(data, CultureInfo.InvariantCulture).PadLeft(field.Length, ' ');
            if (text.Length > field.Length)
            {
                text.Substring(0, field.Length);
            }

            return context.Encoding.GetBytes(text);
        }

        /// <inheritdoc />
        public object Decode(EncoderContext context, byte[] buffer)
        {
            string text = context.Encoding.GetString(buffer).Trim();
            if (text.Length == 0)
            {
                return null;
            }

            return Convert.ToSingle(text, CultureInfo.InvariantCulture);
        }
    }
}