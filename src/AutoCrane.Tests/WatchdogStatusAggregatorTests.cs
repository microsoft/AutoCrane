using AutoCrane.Models;
using AutoCrane.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace AutoCrane.Tests
{
    [TestClass]
    public class WatchdogStatusAggregatorTests
    {

        [TestMethod]
        public void TestAggregateEmpty()
        {
            var x = new WatchdogStatusAggregator();

            Assert.AreEqual("Unknown", x.Aggregate(new Dictionary<string, string>()));
        }

        [TestMethod]
        public void TestAggregate()
        {
            var x = new WatchdogStatusAggregator();
            var d = new Dictionary<string, string>()
            {
                ["a"] = "Error/...",
                [WatchdogStatus.Prefix + "a"] = "Info/...",
                [WatchdogStatus.Prefix + "b"] = "Warning/...",
                [WatchdogStatus.Prefix + "c"] = "Unknown/...",
            };

            Assert.AreEqual("Warning", x.Aggregate(d));
        }

        [DataRow("Unknown", "Unknown", "Unknown")]
        [DataRow("Unknown", "Info", "Info")]
        [DataRow("Info", "Unknown", "Info")]
        [DataRow("Unknown", "Warning", "Warning")]
        [DataRow("Warning", "Info", "Warning")]
        [DataRow("Error", "Warning", "Error")]
        [DataRow("Info", "Error", "Error")]
        [DataTestMethod]
        public void TestWorseStatus(string s1, string s2, string r)
        {
            var x = new WatchdogStatusAggregator();
            
            Assert.AreEqual(r, x.MoreCriticalStatus(s1, s2));
        }

        [TestMethod]
        public void TestStableUnknown()
        {
            var x = new WatchdogStatusAggregator();

            Assert.AreEqual("x", x.MoreCriticalStatus(x.MoreCriticalStatus("x", "y"), "z"));
        }

    }
}
