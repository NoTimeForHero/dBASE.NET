using System;
using System.Globalization;
using System.IO;
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
                        for (int column = 0; column < fixture.Fields.Length; column++)
                        {
                            var field = fixture.Fields[column];
                            var value = ToDbfValue(field, expected.Values[column]);
                            var label = $"{name}: record {expected.Record}, field {field.Name}";
                            Assert.AreEqual(value, record[column], label);
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
            }
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
