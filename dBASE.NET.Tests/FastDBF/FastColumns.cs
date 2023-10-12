using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace dBASE.NET.Tests.FastDBF
{
    [TestClass]
    public class FastColumns
    {

        private void PrepareFile(string target)
        {
            var dbf = new Dbf();
            dbf.Read("fixtures/memo/dbt/simple.dbf");
            //dbf.Read("columns20.dbf");
            dbf.Write(target);
        }

        [TestMethod]
        public void TestRead()
        {
            PrepareFile("fastcol_2.dbf");
            var fast = FastDbf.ReadFile("fastcol_2.dbf");

            var col1Values = new[] { "User1", "User2", "User3" };
            var col2Values = new[] { "First user", "Second user", "Third user" };

            for (var i = 0; i < 3; i++)
            {
                Assert.AreEqual(col1Values[i], fast.GetValue(i, 0));
                Assert.AreEqual(col2Values[i], fast.GetValue(i, 1));
            }

            Helpers.AssertThrows<ArgumentOutOfRangeException>(() => fast.GetValue(5, 0));
            Helpers.AssertThrows<ArgumentOutOfRangeException>(() => fast.GetValue(0, 5));
        }

        [TestMethod]
        public void TestDeleted()
        {
            var fast = FastDbf.ReadFile("fixtures/deleted/data.dbf");
            Assert.IsFalse(fast.IsDeleted(0), "Record 1 is NOT deleted");
            Assert.IsFalse(fast.IsDeleted(1), "Record 2 is NOT deleted");
            Assert.IsTrue(fast.IsDeleted(2), "Record 3 is deleted");
            Assert.IsTrue(fast.IsDeleted(3), "Record 4 is deleted");
            Assert.IsFalse(fast.IsDeleted(5), "Record 5 is NOT deleted");
            fast.Dispose();
        }

        [TestMethod]
        public void TestReadonly()
        {
            var fast = FastDbf.ReadFile("fixtures/deleted/data.dbf", readOnly: true);
            Helpers.AssertThrows<InvalidOperationException>(() => fast.CreateRecord());
            Helpers.AssertThrows<InvalidOperationException>(() => fast.AppendRecord(null));
            Helpers.AssertThrows<InvalidOperationException>(() => fast.WriteRecord(24, null));
            fast.Dispose();
        }

        [TestMethod]
        public void TestWrite()
        {
            var filename = "fastcol_3.dbf";
            PrepareFile(filename);
            var fast = FastDbf.ReadFile(filename);

            var name = "Victor";
            var data = "This is new information!";

            fast.SetValue(0, 0, name);
            fast.SetValue(0, 1, data);
            fast.Dispose();

            var dbf = new Dbf();
            dbf.Read(filename);

            var record = dbf.Records[0];
            Assert.AreEqual(name, record.Data[0]);
            Assert.AreEqual(data, record.Data[1]);
        }

    }
}
