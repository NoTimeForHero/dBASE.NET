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
        public object Decode(EncoderContext context, ArraySegment<byte> field)
        {
            if (context.CanDecodeAsciiFields && field.Count == 8 && TryDecodeAsciiDate(field, out object date))
                return date;

            string text = context.Encoding.GetString(field.Array, field.Offset, field.Count).Trim();
            if (text.Length == 0) return null;
            return DateTime.ParseExact(text, format, CultureInfo.InvariantCulture);
        }

        private static bool TryDecodeAsciiDate(ArraySegment<byte> field, out object date)
        {
            date = null;
            var bytes = field.Array;
            int end = field.Offset + field.Count;
            if (bytes[field.Offset] == (byte)' ')
            {
                for (int i = field.Offset + 1; i < end; i++)
                {
                    if (bytes[i] != (byte)' ')
                        return false;
                }
                return true;
            }

            int value = 0;
            for (int i = field.Offset; i < end; i++)
            {
                int digit = bytes[i] - (byte)'0';
                if ((uint)digit > 9)
                    return false;
                value = value * 10 + digit;
            }

            int year = value / 10000;
            int month = value / 100 % 100;
            int day = value % 100;
            if (year == 0 || month < 1 || month > 12 || day < 1 || day > DateTime.DaysInMonth(year, month))
                return false;

            date = new DateTime(year, month, day);
            return true;
        }
    }
}
