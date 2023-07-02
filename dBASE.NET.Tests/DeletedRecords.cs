using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace dBASE.NET.Tests
{
	/// <summary>
	/// DBase3WithoutMemo is version 0x03.
	/// </summary>
	[TestClass]
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
            var lDbf = getDbf();
            lDbf.Records.ForEach(x => x.IsDeleted = true);

            using (var stream = new MemoryStream())
            {
                lDbf.Write(stream, DbfVersion.FoxBaseDBase3NoMemo);
                stream.Seek(0, SeekOrigin.Begin);

                var newDbf = new Dbf();
                newDbf.Read(stream);

                Assert.AreEqual(RECORDS_TOTAL, lDbf.Records.Count);
                var deleted = lDbf.Records.Count(x => x.IsDeleted);
                Assert.AreEqual(RECORDS_TOTAL, deleted);
            }
        }
    }
}
