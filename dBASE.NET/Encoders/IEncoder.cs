using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dBASE.NET.Encoders
{
    internal interface IEncoder
    {
        byte[] Encode(DbfField field, object data, Encoding encoding, MemoContext memo);

        object Decode(byte[] buffer, Encoding encoding, MemoContext memo);
    }
}