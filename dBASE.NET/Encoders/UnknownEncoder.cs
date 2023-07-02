using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dBASE.NET.Encoders
{
    internal class UnknownEncoder : IEncoder
    {
        private static UnknownEncoder instance;

        private UnknownEncoder() { }

        public static UnknownEncoder Instance => instance ?? (instance = new UnknownEncoder());

        public byte[] Encode(DbfField field, object data, Encoding encoding)
        {
            if (!(data is byte[] rawBuffer)) throw new ArgumentException("Input object is not a byte[]!");
            return rawBuffer;
        }

        public object Decode(byte[] buffer, byte[] memoData, Encoding encoding) => buffer;
    }
}
