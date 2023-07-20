﻿using System;
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
        private Stream stream;
        private BinaryReader reader;
        private BinaryWriter writer;
        private IMemoAdapter adapter;
        internal MemoFormat Format { get; private set; }


        internal MemoContext PackerInstance { get; private set; }
        internal void BeginPacking(DbfVersion version)
        {
            PackerInstance = new MemoContext();
            PackerInstance.Initialize(new MemoryStream(), version);
        }

        internal void CopyStreamTo(Stream target)
        {
            var currentStream = PackerInstance != null ? PackerInstance.stream : stream;
            currentStream.Position = 0;
            currentStream.CopyTo(target);
        }

        internal void Initialize(Stream stream, DbfVersion version)
        {
            Dispose();
            if (stream == null) return;
            this.stream = stream;
            reader = new BinaryReader(stream);
            writer = new BinaryWriter(stream);
            Format = MemoFormatParser.FromVersion(version);

            switch (version)
            {
                case DbfVersion.FoxBaseDBase3WithMemo:
                    adapter = new Dbase3Adapter();
                    break;
                case DbfVersion.FoxPro2WithMemo:
                case DbfVersion.VisualFoxPro:
                    adapter = new FoxProAdapter();
                    break;
                // DBase IV has different memo format!
                //case DbfVersion.dBase4WithMemo:
                case DbfVersion.Unknown:
                    throw new InvalidOperationException("Unknown Dbase Type!");
                default:
                    throw new NotImplementedException($"DBase type not supported: {version}");
            }
            adapter.Initialize(stream, reader, writer);
        }


        internal MemoContext() { }

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
        public BlockWriteStatusEnum WriteBlockData(int index, byte[] data)
        {
            if (stream == null) return BlockWriteStatusEnum.InvalidStream;
            return adapter.WriteBlockData(index, data);
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
            writer?.Dispose();
            reader?.Dispose();
            PackerInstance?.Dispose();
            PackerInstance = null;
        }
    }
}
