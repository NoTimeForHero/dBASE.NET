namespace dBASE.NET.Tests.Encoders
{
    using System;
    using System.Globalization;
    using System.Text;

    using dBASE.NET.Encoders;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class NumericEncoderTests
    {
        private readonly DbfField numericField = new DbfField("SUMMA", DbfFieldType.Numeric, 10, 2);

        private readonly DbfField integerField = new DbfField("NUM", DbfFieldType.Numeric, 10, 0);

        private readonly Encoding encoding = Encoding.ASCII;

        [TestMethod]
        public void EncodeTestDecimalOverLength()
        {
            // Arrange.
            const decimal val = 13005200m;
            var expectedVal = new[] { '1', '3', '0', '0', '5', '2', '0', '0', '.', '0' };

            // Act.
            var expectedEncodedVal = encoding.GetBytes(expectedVal);
            var context = new EncoderContext { Encoding = encoding, Field = numericField };
            var encodedVal = EncoderFactory.Resolve<NumericEncoder>().Encode(context, val);

            // Assert.
            for (int i = 0; i < numericField.Length; i++)
            {
                Assert.AreEqual(expectedEncodedVal[i], encodedVal[i], $"Position `{i}` failed.");
            }
        }

        [TestMethod]
        public void EncodeTestDecimalOverPrecision()
        {
            // Arrange.
            const decimal val = 1300.5200m;
            var expectedVal = new[] { ' ', ' ', ' ', '1', '3', '0', '0', '.', '5', '2' };

            // Act.
            var expectedEncodedVal = encoding.GetBytes(expectedVal);
            var context = new EncoderContext { Encoding = encoding, Field = numericField };
            var encodedVal = EncoderFactory.Resolve<NumericEncoder>().Encode(context, val);

            // Assert.
            for (int i = 0; i < numericField.Length; i++)
            {
                Assert.AreEqual(expectedEncodedVal[i], encodedVal[i], $"Position `{i}` failed.");
            }
        }

        [TestMethod]
        public void EncodeTestDecimalUnderPrecision()
        {
            // Arrange.
            const decimal val = 1300m;
            var expectedVal = new[] { ' ', ' ', ' ', '1', '3', '0', '0', '.', '0', '0' };

            // Act.
            var expectedEncodedVal = encoding.GetBytes(expectedVal);
            var context = new EncoderContext { Encoding = encoding, Field = numericField };
            var encodedVal = EncoderFactory.Resolve<NumericEncoder>().Encode(context, val);

            // Assert.
            for (int i = 0; i < numericField.Length; i++)
            {
                Assert.AreEqual(expectedEncodedVal[i], encodedVal[i], $"Position `{i}` failed.");
            }
        }

        [TestMethod]
        public void EncodeTestDecimalNull()
        {
            // Arrange.
            decimal? val = null;
            string expectedVal = numericField.DefaultValue;

            // Act.
            var expectedEncodedVal = encoding.GetBytes(expectedVal);
            var context = new EncoderContext { Encoding = encoding, Field = numericField };
            var encodedVal = EncoderFactory.Resolve<NumericEncoder>().Encode(context, val);

            // Assert.
            for (int i = 0; i < numericField.Length; i++)
            {
                Assert.AreEqual(expectedEncodedVal[i], encodedVal[i], $"Position `{i}` failed.");
            }
        }

        [TestMethod]
        public void EncodeTestNumber()
        {
            // Arrange.
            const int val = 1;
            var expectedVal = new[] { ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '1' };

            // Act.
            var expectedEncodedVal = encoding.GetBytes(expectedVal);
            var context = new EncoderContext { Encoding = encoding, Field = integerField };
            var encodedVal = EncoderFactory.Resolve<NumericEncoder>().Encode(context, val);

            // Assert.
            for (int i = 0; i < numericField.Length; i++)
            {
                Assert.AreEqual(expectedEncodedVal[i], encodedVal[i], $"Position `{i}` failed.");
            }
        }

        [TestMethod]
        public void DecodeNumericAndFloatMatchInvariantConversion()
        {
#if NET8_0_OR_GREATER
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
            var inputs = new[]
            {
                "", "  ", " \t\r", "   +123   ", " -999999999", "999999999", "1000000000",
                "16777217", " -0 ", "-0.0", "00000000000", "0.01", " -99.25 ", " +.5 ",
                " 1,234.50 ", " 12e-2 ", "NaN", "-Infinity", "1E-999", "1E+100", "1E+999",
                "\u00A012.5\u00A0", "12.5\0 ", " - ", "12x", "12 34", "12.5,0", "*****"
            };

            foreach (var fieldEncoding in new[] { Encoding.ASCII, Encoding.UTF8, Encoding.GetEncoding(866) })
            {
                var context = new EncoderContext { Encoding = fieldEncoding };
                foreach (string input in inputs)
                {
                    var row = fieldEncoding.GetBytes("x" + input + "y");
                    var field = new ArraySegment<byte>(row, 1, row.Length - 2);
                    AssertDecodesLikeConvert(context, field);
                }
            }
        }

        [TestMethod]
        public void DecodeNumericAndFloatRespectCustomEncoding()
        {
            var context = new EncoderContext { Encoding = new RemappedNumericEncoding() };
            var field = new ArraySegment<byte>(new byte[] { (byte)'1' });

            AssertDecodesLikeConvert(context, field);
        }

        private static void AssertDecodesLikeConvert(EncoderContext context, ArraySegment<byte> field)
        {
            string text = context.Encoding.GetString(field.Array, field.Offset, field.Count).Trim();
            var numeric = EncoderFactory.Resolve<NumericEncoder>();
            var floatingPoint = EncoderFactory.Resolve<FloatEncoder>();
            object expectedDouble = ReadResult(() => text.Length == 0 ? null : (object)Convert.ToDouble(text, CultureInfo.InvariantCulture));
            object expectedSingle = ReadResult(() => text.Length == 0 ? null : (object)Convert.ToSingle(text, CultureInfo.InvariantCulture));
            object actualDouble = ReadResult(() => numeric.Decode(context, field));
            object actualSingle = ReadResult(() => floatingPoint.Decode(context, field));

            Assert.AreEqual(expectedDouble, actualDouble, text);
            Assert.AreEqual(expectedSingle, actualSingle, text);
            if (expectedDouble is double doubleValue)
                Assert.AreEqual(BitConverter.DoubleToInt64Bits(doubleValue), BitConverter.DoubleToInt64Bits((double)actualDouble), text);
            if (expectedSingle is float singleValue)
                CollectionAssert.AreEqual(BitConverter.GetBytes(singleValue), BitConverter.GetBytes((float)actualSingle), text);
        }

        private static object ReadResult(Func<object> read)
        {
            try
            {
                return read();
            }
            catch (Exception exception) when (exception is FormatException || exception is OverflowException)
            {
                return exception.GetType();
            }
        }

        private sealed class RemappedNumericEncoding : UTF8Encoding
        {
            public override string GetString(byte[] bytes, int index, int count) => "-17.25";
        }
    }
}
