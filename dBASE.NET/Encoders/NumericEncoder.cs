namespace dBASE.NET.Encoders
{
    using System;
    using System.Globalization;
    using System.Text;

    internal class NumericEncoder : IEncoder
    {
        /// <inheritdoc />
        public byte[] Encode(EncoderContext context, object data)
        {
            string text = Convert.ToString(data, CultureInfo.InvariantCulture);
            if (string.IsNullOrEmpty(text))
            {
                text = context.Field.DefaultValue;
            }
            else
            {
                var parts = text.Split('.');
                if (parts.Length == 2)
                {
                    // Truncate or pad float part.
                    if (parts[1].Length > context.Field.Precision)
                    {
                        parts[1] = parts[1].Substring(0, context.Field.Precision);
                    }
                    else
                    {
                        parts[1] = parts[1].PadRight(context.Field.Precision, '0');
                    }
                }
                else if (context.Field.Precision > 0)
                {
                    // If value has no fractional part, pad it with zeros.
                    parts = new[] { parts[0], new string('0', context.Field.Precision) };
                }

                text = string.Join(".", parts);

                // Pad string with spaces or trim.
                text = text.Length > context.Field.Length
                    ? text.Substring(0, context.Field.Length)
                    : text.PadLeft(context.Field.Length, ' ');
            }

            return context.Encoding.GetBytes(text);
        }

        /// <inheritdoc />
        public object Decode(EncoderContext context, ArraySegment<byte> field)
        {
            if (field.Count != 0 && context.CanDecodeAsciiFields)
            {
                var unpadded = FieldBytes.TrimAsciiSpaces(field);
                if (unpadded.Count == 0)
                    return null;
                if (FieldBytes.TryDecodeAsciiInteger(unpadded, out int integer))
                    return (double)integer;

                if (FieldBytes.IsAscii(unpadded))
                {
#if NET8_0_OR_GREATER
                    if (double.TryParse(unpadded.AsSpan(), CultureInfo.InvariantCulture, out double value))
                        return value;
#endif
                    field = unpadded;
                }
            }

            string text = context.Encoding.GetString(field.Array, field.Offset, field.Count).Trim();
            if (text.Length == 0)
            {
                return null;
            }

            return Convert.ToDouble(text, CultureInfo.InvariantCulture);
        }
    }
}
