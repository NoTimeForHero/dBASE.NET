using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace dBASE.NET.Tests.Memo
{
    [TestClass]
    public class WrongFields
    {

        [TestMethod]
        public void WrongCreate()
        {
            var dbf = new Dbf();
            var field = new DbfField("memo", DbfFieldType.Memo, 10);
            Helpers.AssertThrows<ArgumentException>(() =>
            {
                dbf.Create(new[] { field }, DbfVersion.FoxBaseDBase3NoMemo);
            });
        }

        [TestMethod]
        public void SimpleRead()
        {
            Helpers.AssertThrows<InvalidOperationException>(() =>
            {
                using var stream = new FileStream("fixtures/memo/simple.dbf", FileMode.Open, FileAccess.Read);
                var dbf = new Dbf();
                dbf.Read(stream);
            });
        }

    }
}
