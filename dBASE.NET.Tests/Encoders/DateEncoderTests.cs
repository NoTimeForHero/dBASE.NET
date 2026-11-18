namespace dBASE.NET.Tests.Encoders
{
    using System;
    using System.Text;

    using dBASE.NET.Encoders;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DateEncoderTests
    {
        private readonly DbfField dateField = new DbfField("DATADOC", DbfFieldType.Date, 8);

        private readonly Encoding encoding = Encoding.ASCII;

        [TestMethod]
        public void EncodeTestDateValid()
        {
            // Arrange.
            var val = new DateTime(1939, 09, 01);
            var expectedVal = new[] { '1', '9', '3', '9', '0', '9', '0', '1' };

            // Act.
            var expectedEncodedVal = encoding.GetBytes(expectedVal);
            var context = new EncoderContext { Encoding = encoding, Field = dateField };
            var encodedVal = EncoderFactory.Resolve<DateEncoder>().Encode(context, val);

            // Assert.
            for (int i = 0; i < dateField.Length; i++)
            {
                Assert.AreEqual(expectedEncodedVal[i], encodedVal[i], $"Position `{i}` failed.");
            }
        }

        [TestMethod]
        public void EncodeTestDateNull()
        {
            // Arrange.
            DateTime? val = null;
            string expectedVal = dateField.DefaultValue;

            // Act.
            var expectedEncodedVal = encoding.GetBytes(expectedVal);
            var context = new EncoderContext { Encoding = encoding, Field = dateField };
            var encodedVal = EncoderFactory.Resolve<DateEncoder>().Encode(context, val);

            // Assert.
            for (int i = 0; i < dateField.Length; i++)
            {
                Assert.AreEqual(expectedEncodedVal[i], encodedVal[i], $"Position `{i}` failed.");
            }
        }

        [TestMethod]
        public void DecodeAsciiDateAndBlankAtFieldOffset()
        {
            var decoder = EncoderFactory.Resolve<DateEncoder>();
            var context = new EncoderContext { Encoding = Encoding.GetEncoding(866), Field = dateField };

            var row = context.Encoding.GetBytes("X20240229        Y");
            var decoded = (DateTime)decoder.Decode(context, new ArraySegment<byte>(row, 1, 8));
            Assert.AreEqual(new DateTime(2024, 2, 29), decoded);
            Assert.AreEqual(DateTimeKind.Unspecified, decoded.Kind);
            Assert.IsNull(decoder.Decode(context, new ArraySegment<byte>(row, 9, 8)));
        }

        [TestMethod]
        public void DecodeInvalidAsciiDatesStillThrowsFormatException()
        {
            var decoder = EncoderFactory.Resolve<DateEncoder>();
            var context = new EncoderContext { Encoding = encoding, Field = dateField };

            foreach (var text in new[] { "20230229", "20240010", "20241301", "00000101", "20X40229" })
            {
                var field = new ArraySegment<byte>(encoding.GetBytes(text));
                Helpers.AssertThrows<FormatException>(() => decoder.Decode(context, field));
            }
        }

        [TestMethod]
        public void DecodePaddedDatesAndUnicodeUsesTheOriginalRules()
        {
            var decoder = EncoderFactory.Resolve<DateEncoder>();
            var context = new EncoderContext { Encoding = Encoding.Unicode, Field = dateField };

            foreach (var fieldEncoding in new[] { Encoding.Unicode, Encoding.UTF8, Encoding.ASCII })
            {
                context.Encoding = fieldEncoding;
                var field = new ArraySegment<byte>(fieldEncoding.GetBytes(" 20240229 "));
                Assert.AreEqual(new DateTime(2024, 2, 29), decoder.Decode(context, field));
            }
        }
    }
}
