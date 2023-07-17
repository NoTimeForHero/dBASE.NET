using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace dBASE.NET.Tests
{
    internal class Helpers
    {
        public static TException AssertThrows<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                Assert.Fail($"Got wrong Exception, expected: {typeof(TException).FullName}, got: {ex.GetType().FullName}");
                return null;
            }
            Assert.Fail($"Test method did not throw expected Exception of type: {typeof(TException).FullName}");
            return null;
        }
    }
}
