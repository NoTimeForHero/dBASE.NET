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
    public class PackMemoTests
    {

        public void ResizeRecord(DbfVersion version, string prefix, string ext, int sizeAfterPack)
        {
            Dbf dbf;
            var TEST_LABEL = "Hello world!";

            dbf = new Dbf();
            var field = new DbfField("TEST_MEMO", DbfFieldType.Memo, 10);
            dbf.Create(new[] { field }, version);
            DbfRecord record = dbf.CreateRecord();
            record.Data[0] = TEST_LABEL;

            for (int i = 0; i <= 11; i++)
            {
                using var msDataInner = new MemoryStream();
                using var msMemoInner = new MemoryStream();

                var targetChar = (char)('a' - 1 + i);

                Console.WriteLine($"Overwrite number: {i}");
                dbf.Write(msDataInner, version, memoStream: msMemoInner, leaveOpen: true);

                dbf = new Dbf();
                dbf.Read(msDataInner, msMemoInner);
                dbf.Records[0].Data[0] = i == 11 ? TEST_LABEL : new string(targetChar, 500 * i);
            }

            using var msData = new FileStream($"clean_{prefix}.dbf", FileMode.Create, FileAccess.ReadWrite);
            using var msMemo = new FileStream($"clean_{prefix}.{ext}", FileMode.Create, FileAccess.ReadWrite);
            // Console.WriteLine("Result data: " + dbf.Records[0].Data[0]);
            dbf.Write(msData, version, memoStream: msMemo, leaveOpen: true, packRecords: true);

            dbf = new Dbf();
            dbf.Read(msData, msMemo);

            Assert.AreEqual(1, dbf.Records.Count);
            Assert.AreEqual(TEST_LABEL, dbf.Records[0].Data[0]);
            Assert.AreEqual(sizeAfterPack, msMemo.Length, "Wrong memo file length after packing!");
        }

        // Before compression:
        // DBT: 28554
        // FPT: 28928

        // After compression:
        // DBT: 526
        // FPT: 1088

        [TestMethod]
        public void ResizeRecord_DBT() => ResizeRecord(DbfVersion.FoxBaseDBase3WithMemo, "D", "dbt", 526);

        [TestMethod]
        public void ResizeRecord_FPT() => ResizeRecord(DbfVersion.FoxPro2WithMemo, "F", "fpt", 1088);

        public void CleanRemoved(DbfVersion version, string prefix, string ext, int sizeAfterPack)
        {
            Dbf dbf;
            DbfRecord record;
            var TEST_LABEL = "Hello world!";
            var REC_COUNT = 100;

            dbf = new Dbf();
            var field = new DbfField("TEST_MEMO", DbfFieldType.Memo, 10);
            dbf.Create(new[] { field }, version);

            for (var i = 0; i < REC_COUNT; i++)
            {
                record = dbf.CreateRecord();
                record.IsDeleted = true;
                record.Data[0] = $"Test record number: {i+1}";
            }

            record = dbf.CreateRecord();
            record.Data[0] = TEST_LABEL;

            using var msDataFast = new MemoryStream();
            using var msMemoFast = new MemoryStream();
            dbf.Write(msDataFast, version, memoStream: msMemoFast, leaveOpen: true, packRecords: false);

            dbf = new Dbf();
            dbf.Read(msDataFast, msMemoFast);
            Assert.AreEqual(REC_COUNT + 1, dbf.Records.Count, "Record count without packing");
            Console.WriteLine("Size before compression: " + msMemoFast.Length);

            using var msData = new FileStream($"clean2_{prefix}.dbf", FileMode.Create, FileAccess.ReadWrite);
            using var msMemo = new FileStream($"clean2_{prefix}.{ext}", FileMode.Create, FileAccess.ReadWrite);
            dbf.Write(msData, version, memoStream: msMemo, leaveOpen: true, packRecords: true);
            dbf = new Dbf();
            dbf.Read(msData, msMemo);
            Console.WriteLine("Size after compression: " + msMemo.Length);
            Assert.AreEqual(1, dbf.Records.Count, "Record count after packing");
            Assert.AreEqual(sizeAfterPack, msMemo.Length, "Wrong memo file length after packing!");
        }

        [TestMethod]
        public void CleanRemoved_DBT() => CleanRemoved(DbfVersion.FoxBaseDBase3WithMemo, "D", "dbt", 526);

        [TestMethod]
        public void CleanRemoved_FPT() => CleanRemoved(DbfVersion.FoxPro2WithMemo, "F", "fpt", 1088);

    }
}
