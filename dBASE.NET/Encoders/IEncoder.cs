using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dBASE.NET.Encoders
{
    internal interface IEncoder
    {
        byte[] Encode(EncoderContext context, object data);
        object Decode(EncoderContext context, byte[] buffer);
    }
}