using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dBASE.NET.Encoders
{
    public class EncoderContext
    {
        public Encoding Encoding { get; set; }
        public MemoContext Memo { get; set; }
        public DbfField Field { get; set; }

        public readonly Dictionary<object, object> RecordContext = new();
    }
}
