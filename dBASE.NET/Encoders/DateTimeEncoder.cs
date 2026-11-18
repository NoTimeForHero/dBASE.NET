namespace dBASE.NET.Encoders
{
    using System;
    using System.IO;

    internal class DateTimeEncoder : IEncoder
    {
        /// <inheritdoc />
        public byte[] Encode(EncoderContext context, object data)
        {
            if (context.Field.Length != 8) throw new ArgumentException("DateTime fields must always be 8 bytes in length.");

            // Null values result in zeroes.
            if (data == null) return new byte[context.Field.Length];
            if (context.Field.Type == DbfFieldType.UnixTime && data is long rawTimestamp)
                return BitConverter.GetBytes(rawTimestamp);

            // The date gets encoded as a Julian Day.
            DateTime dt = (DateTime)data;
            ulong date = DateToJulian(dt);

            ulong time = (ulong)(dt.TimeOfDay.Ticks / TimeSpan.TicksPerMillisecond);
            if (context.Field.Type == DbfFieldType.DateTime) time -= time % 1000;

            // Date and time are encoded as two uint32 double words.
            return BitConverter.GetBytes(date | (time << 32));
        }

        /// <summary>
        /// Convert a .NET DateTime structure to a Julian Day Number as long
        /// Implemented from pseudo code at http://en.wikipedia.org/wiki/Julian_day
        /// </summary>
        private static ulong DateToJulian(DateTime date)
        {
            ulong jdn = (ulong)((1461 * (date.Year + 4800 + (date.Month - 14) / 12)) / 4
                                + (367 * (date.Month - 2 - 12 * ((date.Month - 14) / 12))) / 12
                                - (3 * ((date.Year + 4900 + (date.Month - 14) / 12) / 100)) / 4 + date.Day - 32075);
            return jdn;
        }

        /// <inheritdoc />
        public object Decode(EncoderContext context, ArraySegment<byte> field)
        {
            FieldBytes.RequireWidth(field, 2 * sizeof(uint));
            uint dateWord = BitConverter.ToUInt32(field.Array, field.Offset);
            uint milliseconds = BitConverter.ToUInt32(field.Array, field.Offset + sizeof(uint));
            if (milliseconds >= 86400000) throw new InvalidDataException("Invalid DBF timestamp time of day.");

            if (context.Field.Type == DbfFieldType.DateTime)
                return JulianToDateTime(dateWord).AddMilliseconds(milliseconds - milliseconds % 1000);
            if (dateWord == 0 && milliseconds == 0) return null;
            return JulianToDateTime(dateWord).AddMilliseconds(milliseconds);
        }

        /// <summary>
        /// Convert a Julian Date as long to a .NET DateTime structure
        /// Implemented from pseudo code at http://en.wikipedia.org/wiki/Julian_day
        /// </summary>
        /// <param name="julianDateAsLong">Julian Date to convert (days since 01/01/4713 BC)</param>
        /// <returns>DateTime</returns>
        private static DateTime JulianToDateTime(long julianDateAsLong)
        {
            if (julianDateAsLong == 0) return DateTime.MinValue;
            double p = Convert.ToDouble(julianDateAsLong);
            double s1 = p + 68569;
            double n = Math.Floor(4 * s1 / 146097);
            double s2 = s1 - Math.Floor(((146097 * n) + 3) / 4);
            double i = Math.Floor(4000 * (s2 + 1) / 1461001);
            double s3 = s2 - Math.Floor(1461 * i / 4) + 31;
            double q = Math.Floor(80 * s3 / 2447);
            double d = s3 - Math.Floor(2447 * q / 80);
            double s4 = Math.Floor(q / 11);
            double m = q + 2 - (12 * s4);
            double j = (100 * (n - 49)) + i + s4;
            return new DateTime(Convert.ToInt32(j), Convert.ToInt32(m), Convert.ToInt32(d));
        }
    }
}
