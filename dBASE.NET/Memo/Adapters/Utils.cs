using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dBASE.NET.Memo.Adapters
{
    internal class Utils
    {
        public static int BlocksNeededToFit(int blockSize, int length)
        {
            int fullBlocks = length / blockSize;
            int mod = length % blockSize;
            if (mod > 0) fullBlocks += 1;
            return fullBlocks;
        }

        public static int LeftSizeInBlock(int blockSize, int length)
        {
            int blocksBytes = blockSize * BlocksNeededToFit(blockSize, length);
            return blocksBytes - length;
        }
    }
}
