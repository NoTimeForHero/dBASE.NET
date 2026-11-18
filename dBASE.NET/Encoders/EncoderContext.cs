using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dBASE.NET.Encoders
{
    public class EncoderContext
    {
        private Encoding encoding;

        public Encoding Encoding
        {
            get => encoding;
            set
            {
                if (ReferenceEquals(encoding, value))
                    return;

                encoding = value;
                CanDecodeAsciiFields = FieldBytes.HasStandardAsciiBytes(value);
            }
        }

        internal bool CanDecodeAsciiFields { get; private set; }
        public MemoContext Memo { get; set; }
        public DbfField Field { get; set; }

        public readonly Dictionary<object, object> RecordContext = new();
    }
}
