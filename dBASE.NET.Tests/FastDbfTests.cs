using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace dBASE.NET.Tests
{
    [TestClass]
    public class FastDbfTests
    {
        private void PrepareFile(string target)
        {
            var dbf = new Dbf();
            dbf.Read("fixtures/memo/dbt/simple.dbf");
            dbf.Write(target);
        }

        [TestMethod]
        public void TestCount()
        {
            PrepareFile("fast_1.dbf");
            var fast = FastDbf.ReadFile("fast_1.dbf");
            Assert.AreEqual(3, fast.RecordCount);
            Console.WriteLine("What is going on?!");
        }

        [TestMethod]
        public void TestRead()
        {
            PrepareFile("fast_2.dbf");
            var fast = FastDbf.ReadFile("fast_2.dbf");
            DbfRecord record;

            record = fast.GetRecord(0);
            Assert.AreEqual("User1", record.Data[0]);
            Assert.AreEqual("First user", record.Data[1]);

            record = fast.GetRecord(1);
            Assert.AreEqual("User2", record.Data[0]);
            Assert.AreEqual("Second user", record.Data[1]);

            record = fast.GetRecord(2);
            Assert.AreEqual("User3", record.Data[0]);
            Assert.AreEqual("Third user", record.Data[1]);

            Helpers.AssertThrows<ArgumentOutOfRangeException>(() => fast.GetRecord(3));
        }

        [TestMethod]
        public void TestWrite()
        {
            int recordIndex = 2;
            var filename = "fast_3.dbf";
            PrepareFile(filename);
            var fast = FastDbf.ReadFile(filename);
            var record = fast.GetRecord(recordIndex);

            var name = "Victor";
            var data = "This is new information!";

            record.Data[0] = name;
            record.Data[1] = data;
            fast.WriteRecord(recordIndex, record);
            fast.Dispose();

            var dbf = new Dbf();
            dbf.Read(filename);

            record = dbf.Records[recordIndex];
            Assert.AreEqual(name, record.Data[0]);
            Assert.AreEqual(data, record.Data[1]);
        }

        [TestMethod]
        public void TestAppend()
        {
            var filename = "fast_4.dbf";
            PrepareFile(filename);

            var fast = FastDbf.ReadFile(filename);
            var record = fast.CreateRecord();

            var name = "Victor";
            var data = "This is new information!";
            record.Data[0] = name;
            record.Data[1] = data;

            fast.AppendRecord(record);
            fast.AppendRecord(record);
            Assert.AreEqual(5, fast.RecordCount);
            fast.Dispose();

            var dbf = new Dbf();
            dbf.Read(filename);

            Assert.AreEqual(5, dbf.Records.Count); // Failed because we need update counter in header
            record = dbf.Records[dbf.Records.Count - 2];
            Assert.AreEqual(name, record.Data[0]);
            Assert.AreEqual(data, record.Data[1]);
            record = dbf.Records[dbf.Records.Count - 1];
            Assert.AreEqual(name, record.Data[0]);
            Assert.AreEqual(data, record.Data[1]);
        }
    }
}
