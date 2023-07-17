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
            Assert.AreEqual("Lorem", lorem.Substring(0, 5));
            Assert.AreEqual("Aenean", lorem.Substring(256, 6));
            Assert.AreEqual("penatibus", lorem.Substring(511, 9));
            Assert.AreEqual("laoreet, aliqua", lorem.Substring(1018, 15));
            Assert.AreEqual("duis.", lorem.Substring(1399, 5));
        }

        [TestMethod]
        public void TestOverwrite()
        {
            var dbf = new Dbf();
            dbf.Read("fixtures/memo/large.dbf");

            dbf.Write("test_large.dbf");
        }

        [TestMethod]
        public void TestSmallIncrease()
        {
            var dbf = new Dbf();
            dbf.Read("fixtures/memo/large.dbf");

            var lorem = (string)dbf.Records[2].Data[1];
            lorem += " Never gonna give you up!";
            dbf.Records[2].Data[1] = lorem;

            dbf.Write("large_small.dbf");
            dbf.Read("large_small.dbf");
            var loremNew = (string)dbf.Records[2].Data[1];
            Assert.AreEqual(lorem.Length, loremNew.Length);
        }

        [TestMethod]
        public void TestIncreaseToLimit()
        {
            var dbf = new Dbf();
            dbf.Read("fixtures/memo/large.dbf");

            // We have 132 free bytes to left
            var lorem = (string)dbf.Records[2].Data[1];
            var added = "ABC".Repeat(100).Substring(0, 130);
            lorem += added;
            dbf.Records[2].Data[1] = lorem;

            dbf.Write("large_limit.dbf");
            dbf.Read("large_limit.dbf");
            var loremNew = (string)dbf.Records[2].Data[1];
            Assert.AreEqual(lorem.Length, loremNew.Length);
        }

        [TestMethod]
        public void TestIncreaseLarge()
        {
            var dbf = new Dbf();
            dbf.Read("fixtures/memo/large.dbf");

            // We have 132 free bytes to left
            var lorem = (string)dbf.Records[2].Data[1];
            var added = "Somebody once told me".Repeat(100);
            lorem += added;
            dbf.Records[2].Data[1] = lorem;

            var record = dbf.CreateRecord();
            record.Data[0] = "Small";
            record.Data[1] = "This is a just small text";

            dbf.Write("large_large.dbf");
            dbf.Read("large_large.dbf");
            var loremNew = (string)dbf.Records[2].Data[1];
            Assert.AreEqual(lorem.Length, loremNew.Length);
        }

        [TestMethod]
        public void TestAppendLarge()
        {
            var dbf = new Dbf();
            dbf.Read("fixtures/memo/large.dbf");

            var record = dbf.CreateRecord();
            var memo = "Somebody once told me".Repeat(40);
            record.Data[0] = "Large";
            record.Data[1] = memo;

            record = dbf.CreateRecord();
            record.Data[0] = "Small";
            record.Data[1] = "This is a just small text";

            dbf.Write("large_append.dbf");
            dbf.Read("large_append.dbf");
            var memoNew = (string)dbf.Records[dbf.Records.Count - 2].Data[1];
            Assert.AreEqual(memo.Length, memoNew.Length);
        }

    }
}
