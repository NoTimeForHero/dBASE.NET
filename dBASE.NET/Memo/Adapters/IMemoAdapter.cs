using System.IO;
using System.Text;

namespace dBASE.NET.Memo.Adapters
{
    internal interface IMemoAdapter
    {
        void Initialize(Stream stream);
        string GetBlockData(int index, Encoding encoding);
        void WriteBlockData(int index, byte[] data);
    }
}
