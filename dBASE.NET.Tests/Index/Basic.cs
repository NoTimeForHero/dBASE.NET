using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dBASE.NET.Index;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace dBASE.NET.Tests.Index
{
    [TestClass]
    public class Basic
    {


        [TestMethod]
        public void Open()
        {
            using var stream = new FileStream("fixtures/cdx/TEST.cdx", FileMode.Open);
            var view = new IndexView(stream);

        }



    }
}
