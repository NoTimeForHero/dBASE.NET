namespace dBASE.NET.Encoders
{
    using System;
    using System.Globalization;
    using System.Text;

    internal class DateEncoder : IEncoder
    {
        private const string format = "yyyyMMdd";

        /// <inheritdoc />
        public byte[] Encode(EncoderContext context, object data)
        {
            string text;
            if (data is DateTime dt)
            {
                text = dt.ToString(format).PadLeft(context.Field.Length, ' ');
            }
            else
            {
                text = context.Field.DefaultValue;
            }

            return context.Encoding.GetBytes(text);
        }

        /// <inheritdoc />
        public object Decode(EncoderContext context, byte[] buffer)
        {
            string text = context.Encoding.GetString(buffer).Trim();
            if (text.Length == 0) return null;
            return DateTime.ParseExact(text, format, CultureInfo.InvariantCulture);
        }
    }
}