using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace dBASE.NET.Tests.FastDBF
{
    [TestClass]
    public class HarbourFixtures
    {
        private static readonly Encoding Cp866 = Encoding.GetEncoding(866);
        private static readonly string FixtureDirectory = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "fixtures", "harbour", "data");

        [TestMethod]
        public void PhysicalRecordsAndValuesMatchHarbour()
        {
            foreach (var name in new[] { "basic", "special", "numeric", "empty", "single", "test_main" })
            {
                var fixture = LoadFixture(name);
                using (var dbf = FastDbf.ReadFile(FixturePath(name, "dbf"), Cp866, readOnly: true))
                {
                    Assert.AreEqual(fixture.Rows.Length, dbf.RecordCount, name);
                    Assert.AreEqual(fixture.Fields.Length, dbf.Fields.Count, name);
                    var scanned = dbf.ReadRecords().ToArray();
                    var ordinals = Enumerable.Range(0, fixture.Fields.Length).Reverse().ToArray();
                    var projected = dbf.ReadValues(ordinals).ToArray();
                    Assert.AreEqual(fixture.Rows.Length, scanned.Length, name);
                    Assert.AreEqual(fixture.Rows.Length, projected.Length, name);
                    for (int column = 0; column < fixture.Fields.Length; column++)
                    {
                        var expected = fixture.Fields[column];
                        var actual = dbf.Fields[column];
                        Assert.AreEqual(expected.Name, actual.Name, name);
                        Assert.AreEqual(expected.Descriptor[0], (char)actual.Type, expected.Name);
                        Assert.AreEqual(expected.Length, (int)actual.Length, expected.Name);
                        Assert.AreEqual(expected.Decimals, (int)actual.Precision, expected.Name);
                    }

                    for (int row = 0; row < fixture.Rows.Length; row++)
                    {
                        var expected = fixture.Rows[row];
                        var record = dbf.GetRecord(row);
                        Assert.AreEqual(row + 1, expected.Record, name);
                        Assert.AreEqual(expected.Deleted, record.IsDeleted, name);
                        Assert.AreEqual(expected.Deleted, dbf.IsDeleted(row), name);
                        Assert.AreEqual(expected.Deleted, scanned[row].IsDeleted, name);
                        Assert.AreEqual(expected.Deleted, projected[row].IsDeleted, name);
                        Assert.AreEqual(row, projected[row].RowIndex, name);
                        for (int column = 0; column < fixture.Fields.Length; column++)
                        {
                            var field = fixture.Fields[column];
                            var value = ToDbfValue(field, expected.Values[column]);
                            var label = $"{name}: record {expected.Record}, field {field.Name}";
                            Assert.AreEqual(value, record[column], label);
                            Assert.AreEqual(value, scanned[row][column], label);
                            Assert.AreEqual(value, projected[row].Values[ordinals.Length - 1 - column], label);
                            var cell = dbf.GetValue(row, column);
                            Assert.AreEqual(value, cell, label);
                            if (value is DateTime)
                            {
                                Assert.AreEqual(DateTimeKind.Unspecified, ((DateTime)record[column]).Kind, label);
                                Assert.AreEqual(DateTimeKind.Unspecified, ((DateTime)cell).Kind, label);
                            }
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void WideNumericFixtureRetainsExactHarbourValue()
        {
            var fixture = LoadFixture("wide");
            Assert.AreEqual("9007199254740993", fixture.Rows[0].Values[0]);
            using (var dbf = FastDbf.ReadFile(FixturePath("wide", "dbf"), Cp866, readOnly: true))
            {
                Assert.AreEqual(ToDbfValue(fixture.Fields[0], fixture.Rows[0].Values[0]), dbf.GetValue(0, 0));
            }
        }

        [TestMethod]
        public void FileAndStreamReadersDoNotRequireWriteAccess()
        {
            using (var dbfStream = File.Open(FixturePath("basic", "dbf"), FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var fptStream = File.Open(FixturePath("basic", "fpt"), FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var dbf = FastDbf.ReadFile(FixturePath("basic", "dbf"), Cp866, readOnly: true))
                {
                    Assert.AreEqual("  Ёлка  ", dbf.GetValue(0, 5));
                    Helpers.AssertThrows<InvalidOperationException>(() => dbf.SetValue(0, 0, 100));
                }
                using (var dbf = new FastDbf(dbfStream, fptStream, Cp866, readOnly: true))
                {
                    Assert.AreEqual("  Ёлка  ", dbf.GetRecord(0)[5]);
                }
            }
        }

        [TestMethod]
        public void MemoSidecarIsOpenedOnlyForMemoSchema()
        {
            var directory = Path.Combine(Path.GetTempPath(), "dbase-harbour-" + Guid.NewGuid());
            Directory.CreateDirectory(directory);
            try
            {
                var special = Path.Combine(directory, "special.dbf");
                var basic = Path.Combine(directory, "basic.dbf");
                File.Copy(FixturePath("special", "dbf"), special);
                File.Copy(FixturePath("basic", "dbf"), basic);
                var sidecar = Path.ChangeExtension(special, "fpt");
                using (File.Create(sidecar))
                {
                    using (var dbf = FastDbf.ReadFile(special, Cp866, readOnly: true))
                    {
                        Assert.AreEqual(5, dbf.RecordCount);
                    }
                }
                Helpers.AssertThrows<InvalidOperationException>(() => FastDbf.ReadFile(basic, Cp866, readOnly: true));
                using (var dbfStream = File.OpenRead(basic))
                using (var badMemo = new MemoryStream(new byte[7], writable: false))
                {
                    Helpers.AssertThrows<InvalidDataException>(() => new FastDbf(dbfStream, badMemo, Cp866, readOnly: true));
                    Assert.AreEqual(7L, badMemo.Length);
                }
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [TestMethod]
        public void RejectsInvalidPhysicalIndexes()
        {
            using (var empty = FastDbf.ReadFile(FixturePath("empty", "dbf"), Cp866, readOnly: true))
            {
                Helpers.AssertThrows<ArgumentOutOfRangeException>(() => empty.GetRecord(0));
                Helpers.AssertThrows<ArgumentOutOfRangeException>(() => empty.IsDeleted(0));
            }
            using (var single = FastDbf.ReadFile(FixturePath("single", "dbf"), Cp866, readOnly: true))
            {
                foreach (var row in new[] { -1, 1 })
                {
                    Helpers.AssertThrows<ArgumentOutOfRangeException>(() => single.GetRecord(row));
                    Helpers.AssertThrows<ArgumentOutOfRangeException>(() => single.IsDeleted(row));
                    Helpers.AssertThrows<ArgumentOutOfRangeException>(() => single.GetValue(row, 0));
                }
                Helpers.AssertThrows<ArgumentOutOfRangeException>(() => single.GetValue(0, -1));
                Helpers.AssertThrows<ArgumentOutOfRangeException>(() => single.GetValue(0, single.Fields.Count));
                Helpers.AssertThrows<ArgumentOutOfRangeException>(() => single.ReadValues(-1).ToArray());
                Helpers.AssertThrows<ArgumentOutOfRangeException>(() => single.ReadValues(single.Fields.Count).ToArray());
            }
        }

        [TestMethod]
        public void SequentialReadFillsBlocksAcrossShortAndInterleavedReads()
        {
            using var baseStream = new ShortReadStream(File.ReadAllBytes(FixturePath("test_main", "dbf")));
            using var memoStream = File.OpenRead(FixturePath("test_main", "fpt"));
            using var dbf = new FastDbf(baseStream, memoStream, Cp866, readOnly: true);
            int rowIndex = 0;
            int lastIndex = dbf.RecordCount - 1;
            foreach (var row in dbf.ReadValues(0))
            {
                Assert.AreEqual(rowIndex + 1.0, row.Values[0]);
                if (rowIndex % 100 == 0)
                    Assert.AreEqual(lastIndex + 1.0, dbf.GetValue(lastIndex, 0));
                rowIndex++;
            }
            Assert.AreEqual(dbf.RecordCount, rowIndex);
        }

        [TestMethod]
        public void SequentialReadStopsAtAnEndMarkerWithTrailingBytes()
        {
            var bytes = File.ReadAllBytes(FixturePath("numeric", "dbf"));
            int dataOffset = BitConverter.ToUInt16(bytes, 8);
            int recordLength = BitConverter.ToUInt16(bytes, 10);
            bytes[dataOffset + recordLength] = 0x1a;

            using var fast = new FastDbf(new MemoryStream(bytes), readOnly: true);
            Assert.AreEqual(1, fast.ReadRecords().Count());
            Assert.AreEqual(1, fast.ReadValues(0).Count());

            using var dbf = new Dbf();
            dbf.Read(new MemoryStream(bytes));
            Assert.AreEqual(1, dbf.Records.Count);
        }

        [TestMethod]
        public void SequentialReadIgnoresALastRecordWithOnlyAMarker()
        {
            var bytes = File.ReadAllBytes(FixturePath("numeric", "dbf"));
            int dataOffset = BitConverter.ToUInt16(bytes, 8);
            int recordLength = BitConverter.ToUInt16(bytes, 10);
            Array.Resize(ref bytes, dataOffset + recordLength + 1);

            using var fast = new FastDbf(new MemoryStream(bytes), readOnly: true);
            Assert.AreEqual(1, fast.ReadRecords().Count());
            Assert.AreEqual(0, fast.ReadValues().Single().Values.Length);

            using var dbf = new Dbf();
            dbf.Read(new MemoryStream(bytes));
            Assert.AreEqual(1, dbf.Records.Count);
        }

        [TestMethod]
        public void SequentialRecordKeepsMemoPointerAfterTheBlockIsReused()
        {
            using var baseStream = new MemoryStream(File.ReadAllBytes(FixturePath("test_main", "dbf")));
            using var memoStream = new MemoryStream(File.ReadAllBytes(FixturePath("test_main", "fpt")));
            using var dbf = new FastDbf(baseStream, memoStream, Cp866);
            var records = dbf.ReadRecords().ToArray();
            var record = records[0];
            Assert.AreEqual("Оплата за месяц", record["NOTE"]);

            record.Data[8] = "Оплата";
            dbf.WriteRecord(0, record);
            Assert.AreEqual("Оплата", dbf.GetValue(0, 8));
            Assert.AreEqual("Оплата за месяц", dbf.GetValue(7, 8));
        }

        private sealed class ShortReadStream : MemoryStream
        {
            internal ShortReadStream(byte[] bytes) : base(bytes, writable: false) { }

            public override int Read(byte[] buffer, int offset, int count) =>
                base.Read(buffer, offset, Math.Min(count, 29));
        }

        private static string FixturePath(string name, string extension) =>
            Path.Combine(FixtureDirectory, name + "." + extension);

        private static Fixture LoadFixture(string name) =>
            new JavaScriptSerializer().Deserialize<Fixture>(File.ReadAllText(FixturePath(name, "json")));

        private static object ToDbfValue(Field field, object value)
        {
            if (value == null) return null;
            switch (field.Descriptor)
            {
                case "C":
                    var text = ((string)value).TrimEnd(' ');
                    return text.Length == 0 ? null : text;
                case "M":
                    return (string)value == "" ? null : value;
                case "N":
                    return double.Parse((string)value, CultureInfo.InvariantCulture);
                case "+":
                    return int.Parse((string)value, CultureInfo.InvariantCulture);
                case "^":
                    return long.Parse((string)value, CultureInfo.InvariantCulture);
                case "D":
                    return DateTime.ParseExact((string)value, "yyyyMMdd", CultureInfo.InvariantCulture);
                case "@":
                case "=":
                    return DateTime.ParseExact((string)value, "yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
                default:
                    return value;
            }
        }

        public class Fixture
        {
            public Field[] Fields { get; set; }
            public Row[] Rows { get; set; }
        }

        public class Field
        {
            public string Name { get; set; }
            public string Descriptor { get; set; }
            public int Length { get; set; }
            public int Decimals { get; set; }
        }

        public class Row
        {
            public int Record { get; set; }
            public bool Deleted { get; set; }
            public object[] Values { get; set; }
        }
    }
}
