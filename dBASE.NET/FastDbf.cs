using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dBASE.NET
{
    public class FastDbf : BaseDbf, IDisposable
    {
        public FastDbf(Stream inputDbf, Encoding encoding = null, IEnumerable<DbfField> fields = null)
        {

        }

        public void Dispose()
        {
        }
    }
}
