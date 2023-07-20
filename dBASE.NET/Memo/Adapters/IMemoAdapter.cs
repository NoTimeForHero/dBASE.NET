using System.IO;
using System.Text;

namespace dBASE.NET.Memo.Adapters
{
    internal interface IMemoAdapter
    {
        void Initialize(Stream stream, BinaryReader reader, BinaryWriter writer);
        string GetBlockData(int index, Encoding encoding);
        BlockWriteStatusEnum WriteBlockData(int index, byte[] data);
        int AppendBlock(byte[] data);
    }
}
