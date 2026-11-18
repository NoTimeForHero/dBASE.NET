namespace dBASE.NET.Tests.Encoders
{
    using System;
    using System.Text;

    using dBASE.NET.Encoders;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CharacterEncoderTests
    {
        private readonly DbfField characterField = new DbfField("NOMDOC", DbfFieldType.Character, 10);

        private readonly Encoding encoding = Encoding.ASCII;

        [TestMethod]
        public void EncodeTestStringShort()
        {
            // Arrange.
            const string val = "010112.01";
            var expectedVal = new[] { '0', '1', '0', '1', '1', '2', '.', '0', '1', ' ' };

            // Act.
            var expectedEncodedVal = encoding.GetBytes(expectedVal);
            var context = new EncoderContext { Encoding = encoding, Field = characterField };
            var encodedVal = EncoderFactory.Resolve<CharacterEncoder>().Encode(context, val);

            // Assert.
            for (int i = 0; i < characterField.Length; i++)
            {
                Assert.AreEqual(expectedEncodedVal[i], encodedVal[i], $"Position `{i}` failed.");
            }
        }

        [TestMethod]
        public void EncodeTestStringLong()
        {
            // Arrange.
            const string val = "04072016.280";
            var expectedVal = new[] { '0', '4', '0', '7', '2', '0', '1', '6', '.', '2' };

            // Act.
            var expectedEncodedVal = encoding.GetBytes(expectedVal);
            var context = new EncoderContext { Encoding = encoding, Field = characterField };
            var encodedVal = EncoderFactory.Resolve<CharacterEncoder>().Encode(context, val);

            // Assert.
            for (int i = 0; i < characterField.Length; i++)
            {
                Assert.AreEqual(expectedEncodedVal[i], encodedVal[i], $"Position `{i}` failed.");
            }
        }

        [TestMethod]
        public void EncodeTestStringNull()
        {
            // Arrange.
            const string val = null;
            string expectedVal = characterField.DefaultValue;

            // Act.
            var expectedEncodedVal = encoding.GetBytes(expectedVal);
            var context = new EncoderContext { Encoding = encoding, Field = characterField };
            var encodedVal = EncoderFactory.Resolve<CharacterEncoder>().Encode(context, val);

            // Assert.
            for (int i = 0; i < characterField.Length; i++)
            {
                Assert.AreEqual(expectedEncodedVal[i], encodedVal[i], $"Position `{i}` failed.");
            }
        }

        [TestMethod]
        public void DecodeTextMatchesTrimmingDecodedCharacters()
        {
            var decoder = EncoderFactory.Resolve<CharacterEncoder>();
            var inputs = new[] { "", "   ", "\t \r\n", "  \u00A0\tЁлка  дом\u00A0  ", "  A\0B  ", "без пробелов", "  😀  " };
            foreach (var fieldEncoding in new[] { Encoding.ASCII, Encoding.GetEncoding(866), Encoding.GetEncoding(1251), Encoding.UTF8, Encoding.Unicode })
            {
                var context = new EncoderContext { Encoding = fieldEncoding };
                int offset = fieldEncoding.GetByteCount("X");
                foreach (string input in inputs)
                {
                    var row = fieldEncoding.GetBytes("X" + input + "X");
                    int count = row.Length - 2 * offset;
                    string expected = fieldEncoding.GetString(row, offset, count).Trim();
                    Assert.AreEqual(expected.Length == 0 ? null : expected, decoder.Decode(context, new ArraySegment<byte>(row, offset, count)));
                }
            }
        }

        [TestMethod]
        public void DecodeTextPreservesCustomAndStrictDecoders()
        {
            var decoder = EncoderFactory.Resolve<CharacterEncoder>();
            var context = new EncoderContext { Encoding = new PaddingAwareEncoding() };
            var spaces = new ArraySegment<byte>(new byte[] { 0, 32, 32, 0 }, 1, 2);
            Assert.AreEqual("2", decoder.Decode(context, spaces));

            context.Encoding = new UTF8Encoding(false, true);
            var invalid = new ArraySegment<byte>(new byte[] { 0, 32, 32, 0xFF, 32, 0 }, 1, 4);
            try
            {
                context.Encoding.GetString(invalid.Array, invalid.Offset, invalid.Count);
                Assert.Fail("The reference decoder accepted invalid UTF-8.");
            }
            catch (DecoderFallbackException expected)
            {
                try
                {
                    decoder.Decode(context, invalid);
                    Assert.Fail("The field decoder accepted invalid UTF-8.");
                }
                catch (DecoderFallbackException actual)
                {
                    Assert.AreEqual(expected.Index, actual.Index);
                    CollectionAssert.AreEqual(expected.BytesUnknown, actual.BytesUnknown);
                }
            }
        }

        private sealed class PaddingAwareEncoding : UTF8Encoding
        {
            public override string GetString(byte[] bytes, int index, int count) => count.ToString();
        }
    }
}
