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
        public object Decode(EncoderContext context, ArraySegment<byte> field)
        {
            if (field.Count != 0 && context.CanDecodeAsciiFields)
            {
                var unpadded = FieldBytes.TrimAsciiSpaces(field);
                if (unpadded.Count == 0)
                    return null;
                if (FieldBytes.TryDecodeAsciiInteger(unpadded, out int integer))
                    return (float)integer;

                if (FieldBytes.IsAscii(unpadded))
                {
#if NET8_0_OR_GREATER
                    if (float.TryParse(unpadded.AsSpan(), CultureInfo.InvariantCulture, out float value))
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

            return Convert.ToSingle(text, CultureInfo.InvariantCulture);
        }
    }
}
