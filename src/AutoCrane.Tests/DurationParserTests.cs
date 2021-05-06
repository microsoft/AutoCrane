using AutoCrane.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace AutoCrane.Tests
{
    [TestClass]
    public class DurationParserTests
    {

        [DataRow("1d", 60*60*24)]
        [DataRow("3d", 60*60*24*3)]
        [DataRow("3h", 60*60*3)]
        [DataRow("3m", 60*3)]
        [DataRow("3s", 3)]
        [DataRow("3ms", null)]
        [DataRow("", null)]
        [DataRow("1", null)]
        [DataTestMethod]
        public void TestDurations(string str, int? seconds)
        {
            var x = new DurationParser();
            
            var result = x.Parse(str);

            if (seconds == null)
            {
                Assert.IsNull(result);
            }
            else
            {
                Assert.AreEqual(TimeSpan.FromSeconds((double)seconds!), result);
            }
        }
    }
}
