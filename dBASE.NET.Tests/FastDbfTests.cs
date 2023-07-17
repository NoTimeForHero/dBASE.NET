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
            dbf.Read("fixtures/memo/simple.dbf");
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
    }
}
