using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace dBASE.NET.Tests.Memo
{
    [TestClass]
    public class LargeMemoTests
    {
        [TestMethod]
        public void TestRead()
        {
            var dbf = new Dbf();
            dbf.Read("fixtures/memo/large.dbf");

            var lorem = (string)dbf.Records[2].Data[1];
            Assert.AreEqual(1404, lorem.Length);
        }
    }
}
