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
        private const string inputFile = "fixtures/memo/dbt/large.dbf";

        public void TestRead(string path)
        {
            var dbf = new Dbf();
            dbf.Read(path);

            var lorem = (string)dbf.Records[2].Data[1];
            Assert.AreEqual(1404, lorem.Length);
            Assert.AreEqual("Lorem", lorem.Substring(0, 5));
            Assert.AreEqual("Aenean", lorem.Substring(256, 6));
            Assert.AreEqual("penatibus", lorem.Substring(511, 9));
            Assert.AreEqual("laoreet, aliqua", lorem.Substring(1018, 15));
            Assert.AreEqual("duis.", lorem.Substring(1399, 5));
        }

        [TestMethod]
        public void TestRead_DBT() => TestRead("fixtures/memo/dbt/large.dbf");

        [TestMethod]
        public void TestRead_FPT() => TestRead("fixtures/memo/fpt/large.dbf");

        [TestMethod]
        public void TestOverwrite(string path, string prefix)
        {
            var dbf = new Dbf();
            dbf.Read(path);
            dbf.Write($"large_over_{prefix}.dbf");
        }

        [TestMethod]
        public void TestOverwrite_DBT() => TestOverwrite("fixtures/memo/dbt/large.dbf", "D");

        [TestMethod]
        public void TestOverwrite_FPT() => TestOverwrite("fixtures/memo/fpt/large.dbf", "F");

        [TestMethod]
        public void TestSmallIncrease(string path, string prefix)
        {
            var dbf = new Dbf();
            dbf.Read(path);

            var lorem = (string)dbf.Records[2].Data[1];
            lorem += " Never gonna give you up!";
            dbf.Records[2].Data[1] = lorem;

            dbf.Write($"large_small_{prefix}.dbf");
            dbf.Read($"large_small_{prefix}.dbf");
            var loremNew = (string)dbf.Records[2].Data[1];
            Assert.AreEqual(lorem.Length, loremNew.Length);
        }

        [TestMethod]
        public void TestSmallIncrease_DBT() => TestSmallIncrease("fixtures/memo/dbt/large.dbf", "D");

        [TestMethod]
        public void TestSmallIncrease_FPT() => TestSmallIncrease("fixtures/memo/fpt/large.dbf", "F");

        [TestMethod]
        public void TestIncreaseToLimit()
        {
            var dbf = new Dbf();
            dbf.Read(inputFile);

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

        public void TestIncreaseLarge(string path, string prefix)
        {
            var dbf = new Dbf();
            dbf.Read(path);

            // We have 132 free bytes to left
            var lorem = (string)dbf.Records[2].Data[1];
            var added = "Somebody once told me".Repeat(100);
            lorem += added;
            dbf.Records[2].Data[1] = lorem;

            var record = dbf.CreateRecord();
            record.Data[0] = "Small";
            record.Data[1] = "This is a just small text";

            dbf.Write($"large_large_{prefix}.dbf");
            dbf.Read($"large_large_{prefix}.dbf");
            var loremNew = (string)dbf.Records[2].Data[1];
            Assert.AreEqual(lorem.Length, loremNew.Length);
        }

        [TestMethod]
        public void TestIncreaseLarge_DBT() => TestIncreaseLarge("fixtures/memo/dbt/large.dbf", "D");

        [TestMethod]
        public void TestIncreaseLarge_FPT() => TestIncreaseLarge("fixtures/memo/fpt/large.dbf", "F");

        public void TestAppendLarge(string path, string prefix)
        {
            var dbf = new Dbf();
            dbf.Read(path);

            var record = dbf.CreateRecord();
            var memo = "Somebody once told me".Repeat(40);
            record.Data[0] = "Large";
            record.Data[1] = memo;

            record = dbf.CreateRecord();
            record.Data[0] = "Small";
            record.Data[1] = "This is a just small text";

            dbf.Write($"large_append_{prefix}.dbf");
            dbf.Read($"large_append_{prefix}.dbf");
            var memoNew = (string)dbf.Records[dbf.Records.Count - 2].Data[1];
            Assert.AreEqual(memo.Length, memoNew.Length);
        }

        [TestMethod]
        public void TestAppendLarge_DBT() => TestAppendLarge("fixtures/memo/dbt/large.dbf", "D");

        [TestMethod]
        public void TestAppendLarge_FPT() => TestAppendLarge("fixtures/memo/fpt/large.dbf", "F");

    }
}
