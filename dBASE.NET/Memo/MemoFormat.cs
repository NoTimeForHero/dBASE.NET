using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#pragma warning disable CS1591

namespace dBASE.NET.Memo
{
    public enum MemoFormat
    {
        Unknown,
        None,
        DBT,
        FPT
    }

    public static class MemoFormatParser
    {
        public static MemoFormat FromVersion(DbfVersion version)
        {
            switch (version)
            {
                case DbfVersion.dBase4WithMemo:
                case DbfVersion.FoxBaseDBase3WithMemo:
                    return MemoFormat.DBT;
                case DbfVersion.FoxPro2WithMemo:
                case DbfVersion.VisualFoxPro:
                    return MemoFormat.FPT;
                // DBase IV has different memo format!
                //case DbfVersion.dBase4WithMemo:
                case DbfVersion.Unknown:
                    return MemoFormat.Unknown;
                default:
                    return MemoFormat.None;
            }
        }
    }
}
