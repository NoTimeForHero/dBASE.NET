﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            dbf.Records[0].Data[1] = "Hello world!";

            using var ms = new MemoryStream();
            dbf.Write("fixtures/memo/temp.dbf");
        }
    }
}