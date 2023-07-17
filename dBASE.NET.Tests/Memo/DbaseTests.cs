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
    public class DbaseTests
    {
        [TestMethod]
        public void SimpleRead()
        {
            var dbf = new Dbf();
            dbf.Read("fixtures/memo/simple.dbf");

            Assert.AreEqual("First user", dbf.Records[0].Data[1]);
            Assert.AreEqual("Second user", dbf.Records[1].Data[1]);
            Assert.AreEqual("Third user", dbf.Records[2].Data[1]);
        }

        [TestMethod]
        public void SimpleOverwrite()
        {
            var dbf = new Dbf();
            dbf.Read("fixtures/memo/simple.dbf");
            var testMessage = "Hello world!";
            dbf.Records[0].Data[1] = testMessage;

            dbf.Write("fixtures/memo/temp.dbf");

            dbf = new Dbf();
            dbf.Read("fixtures/memo/temp.dbf");
            Assert.AreEqual(testMessage, dbf.Records[0].Data[1]);
        }

        [TestMethod]
        public void SimpleAddRecord()
        {
            var memoData = "Hello world!";

            var dbf = new Dbf();
            dbf.Read("fixtures/memo/simple.dbf");
            var record = dbf.CreateRecord();
            record.Data[0] = "User4";
            record.Data[1] = memoData;

            dbf.Write("fixtures/memo/temp2.dbf");

            dbf = new Dbf();
            dbf.Read("fixtures/memo/temp2.dbf");
            Assert.AreEqual(memoData, dbf.Records[3].Data[1]);
        }

        [TestMethod]
        public void GenerateNewFile()
        {
            Dbf dbf;
            using var msData = new MemoryStream();
            using var msMemo = new MemoryStream();
            //using var msData = new FileStream("c_test.dbf", FileMode.Create, FileAccess.ReadWrite);
            //using var msMemo = new FileStream("c_test.dbt", FileMode.Create, FileAccess.ReadWrite);

            var testData = "Hello world!";

            dbf = new Dbf();
            var field = new DbfField("TEST_MEMO", DbfFieldType.Memo, 10);
            dbf.Create(new[] { field }, DbfVersion.FoxBaseDBase3WithMemo);
            DbfRecord record = dbf.CreateRecord();
            record.Data[0] = testData;

            dbf.Write(msData, DbfVersion.FoxBaseDBase3WithMemo, memoStream: msMemo, leaveOpen: true);

            dbf = new Dbf();
            dbf.Read(msData, msMemo);

            Assert.AreEqual(1, dbf.Records.Count);
            Assert.AreEqual(testData, dbf.Records[0].Data[0]);
        }
    }
}
