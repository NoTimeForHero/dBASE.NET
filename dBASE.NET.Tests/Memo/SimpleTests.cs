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
    public class SimpleTests
    {
        private const string inputFile = "fixtures/memo/dbt/simple.dbf";

        public void SimpleRead(string path)
        {
            var dbf = new Dbf();
            dbf.Read(path);

            Assert.AreEqual("First user", dbf.Records[0].Data[1]);
            Assert.AreEqual("Second user", dbf.Records[1].Data[1]);
            Assert.AreEqual("Third user", dbf.Records[2].Data[1]);
        }

        [TestMethod]
        public void SimpleRead_DBT() => SimpleRead("fixtures/memo/dbt/simple.dbf");

        [TestMethod]
        public void SimpleRead_FPT() => SimpleRead("fixtures/memo/fpt/simple.dbf");


        public void SimpleOverwrite(string path, string prefix)
        {
            var dbf = new Dbf();
            dbf.Read(path);
            var testMessage = "Hello world!";
            dbf.Records[0].Data[1] = testMessage;

            dbf.Write($"temp_{prefix}.dbf");
            dbf.Dispose();
            dbf = new Dbf();
            dbf.Read($"temp_{prefix}.dbf");
            Assert.AreEqual(testMessage, dbf.Records[0].Data[1]);
        }

        [TestMethod]
        public void SimpleOverwrite_DBT() => SimpleOverwrite("fixtures/memo/dbt/simple.dbf", "dbt");

        [TestMethod]
        public void SimpleOverwrite_FPT() => SimpleOverwrite("fixtures/memo/fpt/simple.dbf", "fpt");


        public void SimpleAddRecord(string path, string prefix)
        {
            var memoData = "Hello world!";

            var dbf = new Dbf();
            dbf.Read(path);
            var record = dbf.CreateRecord();
            record.Data[0] = "User4";
            record.Data[1] = memoData;

            dbf.Write($"temp2_{prefix}.dbf");

            dbf = new Dbf();
            dbf.Read($"temp2_{prefix}.dbf");
            Assert.AreEqual(memoData, dbf.Records[3].Data[1]);
        }

        [TestMethod]
        public void SimpleAddRecord_DBT() => SimpleAddRecord("fixtures/memo/dbt/simple.dbf", "dbt");

        [TestMethod]
        public void SimpleAddRecord_FPT() => SimpleAddRecord("fixtures/memo/fpt/simple.dbf", "fpt");

        public void GenerateNewFile(DbfVersion version, string prefix)
        {
            Dbf dbf;
            //using var msData = new MemoryStream();
            //using var msMemo = new MemoryStream();
            using var msData = new FileStream($"temp3_{prefix}.dbf", FileMode.Create, FileAccess.ReadWrite);
            using var msMemo = new FileStream($"temp3_{prefix}.{prefix}", FileMode.Create, FileAccess.ReadWrite);

            var testData = "Hello world!";

            dbf = new Dbf();
            var field = new DbfField("TEST_MEMO", DbfFieldType.Memo, 10);
            dbf.Create(new[] { field }, version);
            DbfRecord record = dbf.CreateRecord();
            record.Data[0] = testData;

            dbf.Write(msData, version, memoStream: msMemo, leaveOpen: true);

            dbf = new Dbf();
            dbf.Read(msData, msMemo);

            Assert.AreEqual(1, dbf.Records.Count);
            Assert.AreEqual(testData, dbf.Records[0].Data[0]);
        }

        [TestMethod]
        public void GenerateNewFile_DBT() => GenerateNewFile(DbfVersion.FoxBaseDBase3WithMemo, "dbt");

        [TestMethod]
        public void GenerateNewFile_FPT() => GenerateNewFile(DbfVersion.FoxPro2WithMemo, "fpt");
    }
}
