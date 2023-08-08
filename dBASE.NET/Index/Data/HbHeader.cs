using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace dBASE.NET.Index.Data
{
    // https://github.com/harbour/core/blob/cd091a696aada3b17bcffbd24994a8c4bddfa674/include/hbrddcdx.h#L188
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [StructLayout(LayoutKind.Sequential)]
    internal struct HbHeader
    {
        public int rootPtr;

        public int freePtr;

        public ushort counter;

        public byte indexOpt;

        public byte indexSignature;

        public ushort headerLen;

        public ushort pageLen;

        public uint signature;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 68)]
        public byte[] reserved2;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 26)]
        public byte[] lang;

        public int collatVer;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 372)]
        public byte[] reserved3;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public byte[] codepage;

        public byte ignoreCase;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] reserved4;

        public ushort ascendFlg;

        public ushort forExpPos;

        public ushort forExpLen;

        public ushort keyExpPos;

        public ushort keyExpLen;
    }
}
