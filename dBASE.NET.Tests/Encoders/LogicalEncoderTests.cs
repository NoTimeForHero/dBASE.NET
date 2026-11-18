using System;
using System.Text;
using dBASE.NET.Encoders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace dBASE.NET.Tests.Encoders
{
    [TestClass]
    public class LogicalEncoderTests
    {
        [TestMethod]
        public void DecodeStandardLogicalMarkersAtFieldOffsets()
        {
            var decoder = EncoderFactory.Resolve<LogicalEncoder>();
            var context = new EncoderContext { Encoding = Encoding.GetEncoding(1251) };
            var row = context.Encoding.GetBytes("YyTtNnFf ?");

            for (int i = 0; i < 4; i++)
                Assert.AreEqual(true, decoder.Decode(context, new ArraySegment<byte>(row, i, 1)));

            for (int i = 4; i < 9; i++)
                Assert.AreEqual(false, decoder.Decode(context, new ArraySegment<byte>(row, i, 1)));

            Assert.IsNull(decoder.Decode(context, new ArraySegment<byte>(row, 9, 1)));
        }

        [TestMethod]
        public void DecodePaddedAndCustomEncodedLogicalValues()
        {
            var decoder = EncoderFactory.Resolve<LogicalEncoder>();
            var context = new EncoderContext { Encoding = Encoding.UTF8 };

            var padded = new ArraySegment<byte>(context.Encoding.GetBytes(" Y "));
            Assert.AreEqual(true, decoder.Decode(context, padded));

            var field = new ArraySegment<byte>(new byte[] { (byte)'Y' });
            Assert.AreEqual(true, decoder.Decode(context, field));
            context.Encoding = new RemappedLogicalEncoding();
            Assert.AreEqual(false, decoder.Decode(context, field));
        }

        private sealed class RemappedLogicalEncoding : UTF8Encoding
        {
            public override string GetString(byte[] bytes, int index, int count) => "N";
        }
    }
}
