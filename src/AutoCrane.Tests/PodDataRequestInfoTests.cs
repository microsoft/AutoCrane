using AutoCrane.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace AutoCrane.Tests
{
    [TestClass]
    public class PodDataRequestInfoTests
    {
        [DataRow("request.data.autocrane.io/test1: x", "test1")]
        [DataRow("request.data.autocrane.io/test1: x;status.data.autocrane.io/test1: y", "test1")]
        [DataRow("request.data.autocrane.io/test1: x;status.data.autocrane.io/test1: x", "")]
        [DataTestMethod]
        public void TestInProgressRequests(string items, string active)
        {
            var dict = items.Split(';').Select(i =>
            {
                var splits = i.Split(':');
                return new KeyValuePair<string, string>(splits[0], splits[1].TrimStart());
            }).ToDictionary(i => i.Key, i => i.Value);

            var activeRequests = active.Split(';');

            var podRequestInfo = new PodDataRequestInfo(new PodIdentifier("a", "b"), dict);
            foreach (var reqName in activeRequests.Where(r => !string.IsNullOrEmpty(r)))
            {
                Assert.IsTrue(podRequestInfo.InProgressRequests.Any(r => r == reqName));
            }
        }
    }
}
