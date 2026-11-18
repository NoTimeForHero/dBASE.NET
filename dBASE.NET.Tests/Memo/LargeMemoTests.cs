using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dBASE.NET.Memo.Adapters;
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

        [TestMethod]
        public void DbtReadHandlesMarkerAcrossBlocksAndShortReads()
        {
            var bytes = new byte[1027];
            var payload = new string('A', 200) + (char)0x1a + new string('B', 310);
            Encoding.ASCII.GetBytes(payload).CopyTo(bytes, 512);
            bytes[1023] = 0x1a;
            bytes[1024] = 0x1a;
            bytes[1025] = (byte)'X';

            using (var stream = new MemoryStream(bytes))
            using (var shortStream = new ShortMemoStream(bytes))
            {
                foreach (var input in new[] { stream, shortStream })
                {
                    var adapter = new Dbase3Adapter();
                    adapter.Initialize(input, null, null);
                    Assert.AreEqual(payload, adapter.GetBlockData(1, Encoding.ASCII));
                    Assert.AreEqual(1023L, input.Position);
                }
            }

            bytes[1024] = 0;
            using var malformed = new MemoryStream(bytes);
            var broken = new Dbase3Adapter();
            broken.Initialize(malformed, null, null);
            Helpers.AssertThrows<InvalidDataException>(() => broken.GetBlockData(1, Encoding.ASCII));
        }

        [TestMethod]
        public void FptReadKeepsWhitespaceAndUsesOnlyAvailablePayload()
        {
            var bytes = new byte[907];
            bytes[7] = 64;
            Encoding.ASCII.GetBytes("Harbour").CopyTo(bytes, 8);
            bytes[518] = 1;
            bytes[519] = 44; // Big-endian length of 300.
            string longMemo = " " + new string('x', 298) + " ";
            Encoding.ASCII.GetBytes(longMemo).CopyTo(bytes, 520);
            bytes[903] = 10; // The last memo has only three bytes before EOF.
            Encoding.ASCII.GetBytes(" y ").CopyTo(bytes, 904);

            using var stream = new ShortMemoStream(bytes);
            using var reader = new BinaryReader(stream);
            var adapter = new FoxProAdapter();
            adapter.Initialize(stream, reader, null);

            Assert.AreEqual(longMemo, adapter.GetBlockData(8, Encoding.ASCII));
            Assert.AreEqual(" y ", adapter.GetBlockData(14, Encoding.ASCII));
            Assert.AreEqual("3", adapter.GetBlockData(14, new LengthReportingEncoding()));
            Assert.AreEqual(longMemo, adapter.GetBlockData(8, Encoding.ASCII));
        }

        private sealed class ShortMemoStream : MemoryStream
        {
            public ShortMemoStream(byte[] bytes) : base(bytes) { }

            public override int Read(byte[] buffer, int offset, int count)
                => base.Read(buffer, offset, Math.Min(count, 13));
        }

        private sealed class LengthReportingEncoding : UTF8Encoding
        {
            public override string GetString(byte[] bytes) => bytes.Length.ToString();
        }
    }
}
