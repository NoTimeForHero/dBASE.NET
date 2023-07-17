using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dBASE.NET.Memo;
using dBASE.NET.Memo.Adapters;

namespace dBASE.NET
{
    /// <summary>
    /// Class for lazy loading memo data
    /// </summary>
    public class MemoContext : IDisposable
    {
        private readonly Stream stream;
        private readonly IMemoAdapter adapter;

        /// <summary>
        /// Return true if file have underlying memo file
        /// </summary>
        public bool HasMemo => stream != null;

        public void CopyStreamTo(Stream target)
        {
            stream.Position = 0;
            stream.CopyTo(target);
        }

        internal MemoContext(Stream stream, DbfHeader header)
        {
            if (stream == null) return;

            this.stream = stream;
            switch (header.Version)
            {
                case DbfVersion.dBase4WithMemo:
                case DbfVersion.FoxBaseDBase3WithMemo:
                    adapter = new DbaseAdapter();
                    break;
                case DbfVersion.FoxPro2WithMemo:
                case DbfVersion.VisualFoxPro:
                    adapter = new FoxProAdapter();
                    break;
                default:
                    throw new NotImplementedException($"DBase type not supported: {header.Version}");
            }
            adapter.Initialize(stream);
        }

        /// <summary>
        /// Parses block with given index
        /// </summary>
        /// <returns>Block data</returns>
        public string GetBlockData(int index, Encoding encoding)
        {
            if (stream == null) return null;
            return adapter.GetBlockData(index, encoding);
        }

        /// <summary>
        /// Write new data to memo file
        /// </summary>
        /// <param name="index">Block index</param>
        /// <param name="data">Raw byte data</param>
        public void WriteBlockData(int index, byte[] data)
        {
            if (stream == null) return;
            adapter.WriteBlockData(index, data);
        }

        /// <summary>
        /// Add new block to memo file
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public int AppendNewBlock(byte[] data)
        {
            return adapter.AppendBlock(data);
        }

        /// <summary>
        /// Close memo stream if he still open
        /// </summary>
        public void Dispose()
        {
            stream?.Dispose();
        }
    }
}
