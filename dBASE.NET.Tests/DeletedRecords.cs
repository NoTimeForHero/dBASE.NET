using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace dBASE.NET.Tests
{
	/// <summary>
	/// DBase3WithoutMemo is version 0x03.
	/// </summary>
	[TestClass]
    [SuppressMessage("ReSharper", "LocalVariableHidesMember")]
    public class DeletedRecords
	{
		Dbf dbf;
        private const int RECORDS_TOTAL = 14;
        private const int RECORDS_DELETED = 6;

        private Dbf getDbf()
        {
            dbf = new Dbf();
            dbf.Read("fixtures/deleted/data.dbf");
            return dbf;
        }

		[TestInitialize]
		public void testInit()
        {
            dbf = getDbf();
        }

		[TestMethod]
		public void RecordCount()
		{
			Assert.AreEqual(RECORDS_TOTAL, dbf.Records.Count, $"Should read {RECORDS_TOTAL} records");

            var deleted = dbf.Records.Count(x => x.IsDeleted);
            Assert.AreEqual(RECORDS_DELETED, deleted, $"Should be {RECORDS_DELETED} deleted records!");
        }

        [TestMethod]
        public void DeletedFlagSavesToFile()
        {
            var dbf = getDbf();
            dbf.Records.ToList().ForEach(x => x.IsDeleted = true);

            using (var stream = new MemoryStream())
            {
                dbf.Write(stream, DbfVersion.FoxBaseDBase3NoMemo, leaveOpen: true);
                stream.Seek(0, SeekOrigin.Begin);

                dbf = new Dbf();
                dbf.Read(stream);

                Assert.AreEqual(RECORDS_TOTAL, dbf.Records.Count);
                var deleted = dbf.Records.Count(x => x.IsDeleted);
                Assert.AreEqual(RECORDS_TOTAL, deleted);
            }
        }

        [TestMethod]
        public void PackRemovedRecords()
        {
            var dbf = getDbf();
            using (var stream = new MemoryStream())
            {
                dbf.Write(stream, DbfVersion.FoxBaseDBase3WithMemo, true, true);
                stream.Seek(0, SeekOrigin.Begin);

                dbf = new Dbf();
                dbf.Read(stream);

                var deleted = dbf.Records.Count(x => x.IsDeleted);
                Assert.AreEqual(0, deleted, "After packing should be zero marked to delete records!");

                var mustBe = RECORDS_TOTAL - RECORDS_DELETED;
                Assert.AreEqual(mustBe, dbf.Records.Count, $"After packing should be {mustBe} records");
            }
        }

    }
}
