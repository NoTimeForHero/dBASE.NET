using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dBASE.NET.Encoders
{
    internal class UnknownEncoder : IEncoder
    {
        public byte[] Encode(EncoderContext context, object data)
        {
            if (!(data is byte[] rawBuffer)) throw new ArgumentException("Input object is not a byte[]!");
            return rawBuffer;
        }

        public object Decode(EncoderContext context, byte[] buffer) => buffer;
    }
}
