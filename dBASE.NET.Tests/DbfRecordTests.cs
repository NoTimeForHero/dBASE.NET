using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace dBASE.NET.Tests
{
    [TestClass]
    public class DbfRecordTests
    {
        Dbf dbf;

        [TestInitialize]
        public void testInit()
        {
            dbf = new Dbf();
            dbf.Read("fixtures/83/dbase_83.dbf");
        }

        [TestMethod]
        public void ShortBinaryFieldDoesNotReadBytesFromTheNextField()
        {
            var header = new Dbf3Header { RecordLength = 5 };
            var fields = new List<DbfField>
            {
                new DbfField("BROKEN", DbfFieldType.Integer, 1),
                new DbfField("NEXT", DbfFieldType.Character, 3)
            };
            using var reader = new BinaryReader(new MemoryStream(new byte[] { 0x20, 1, 65, 66, 67 }));

            Helpers.AssertThrows<ArgumentException>(() => new DbfRecord(reader, header, fields, null, Encoding.ASCII));
        }

        [TestMethod]
        public void ToStringTest()
        {
            // In some locales default float delimiter is , instead . so we need set culture to invariant
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            // Author of this test forget about memo fields
            foreach (var rec in dbf.Records) rec.Data[11] = string.Empty;

            Assert.AreEqual(
                "ID=87,CATCOUNT=2,AGRPCOUNT=0,PGRPCOUNT=0,ORDER=87,CODE=1,NAME=Assorted Petits Fours,THUMBNAIL=graphics/00000001/t_1.jpg,IMAGE=graphics/00000001/1.jpg,PRICE=0,COST=0,DESC=,WEIGHT=5.51,TAXABLE=True,ACTIVE=True",
                dbf.Records[0].ToString(),
                "Default separator and mask");
            Assert.AreEqual(
                "ID=87; CATCOUNT=2; AGRPCOUNT=0; PGRPCOUNT=0; ORDER=87; CODE=1; NAME=Assorted Petits Fours; THUMBNAIL=graphics/00000001/t_1.jpg; IMAGE=graphics/00000001/1.jpg; PRICE=0; COST=0; DESC=; WEIGHT=5.51; TAXABLE=True; ACTIVE=True",
                dbf.Records[0].ToString("; "),
                "Custom separator and default mask");
            Assert.AreEqual(
                "ID->87; CATCOUNT->2; AGRPCOUNT->0; PGRPCOUNT->0; ORDER->87; CODE->1; NAME->Assorted Petits Fours; THUMBNAIL->graphics/00000001/t_1.jpg; IMAGE->graphics/00000001/1.jpg; PRICE->0; COST->0; DESC->; WEIGHT->5.51; TAXABLE->True; ACTIVE->True",
                dbf.Records[0].ToString("; ", "{name}->{value}"),
                "Custom separator and mask");
            Assert.AreEqual(
                "ID; CATCOUNT; AGRPCOUNT; PGRPCOUNT; ORDER; CODE; NAME; THUMBNAIL; IMAGE; PRICE; COST; DESC; WEIGHT; TAXABLE; ACTIVE",
                dbf.Records[0].ToString("; ", "{name}"),
                "Custom separator and mask with only name");
            Assert.AreEqual(
                "87; 2; 0; 0; 87; 1; Assorted Petits Fours; graphics/00000001/t_1.jpg; graphics/00000001/1.jpg; 0; 0; ; 5.51; True; True",
                dbf.Records[0].ToString("; ", "{value}"),
                "Custom separator and mask with only value");
        }
    }
}
