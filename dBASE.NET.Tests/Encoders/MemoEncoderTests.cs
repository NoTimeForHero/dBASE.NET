using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace dBASE.NET.Tests.Encoders
{
    [TestClass]
    public class MemoEncoderTests
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
            var dbf = new Dbf();
            dbf.Read("fixtures/memo/simple.dbf");
            var record = dbf.CreateRecord();
            record.Data[0] = "User";
            record.Data[1] = "Hello world!";

            dbf.Write("fixtures/memo/temp2.dbf");
        }
    }
}