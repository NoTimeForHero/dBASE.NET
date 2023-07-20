using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dBASE.NET.Tests
{
    internal static class Extensions
    {
        public static string Repeat(this string text, uint n) => string.Concat(Enumerable.Repeat(text, (int)n));
        public static string Repeat(this string text, int n) => string.Concat(Enumerable.Repeat(text, n));
    }
}
