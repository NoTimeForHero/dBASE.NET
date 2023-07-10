using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dBASE.NET.Encoders
{
    internal class UnknownEncoder : IEncoder
    {
        public byte[] Encode(DbfField field, object data, Encoding encoding, MemoContext memo)
        {
            if (!(data is byte[] rawBuffer)) throw new ArgumentException("Input object is not a byte[]!");
            return rawBuffer;
        }

        public object Decode(byte[] buffer, Encoding encoding, MemoContext memo) => buffer;
    }
}
