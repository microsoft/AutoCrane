using AutoCrane.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoCrane.Tests
{
    [TestClass]
    public class PodIdentifierFactoryTests
    {

        [DataRow("a", "b", "a/b")]
        [DataRow("default", "a", "a")]
        [DataTestMethod]
        public void TestFromString(string s1, string s2, string r)
        {
            var x = new PodIdentifierFactory();
            
            var pid = x.FromString(r);

            Assert.AreEqual(s1, pid.Namespace);
            Assert.AreEqual(s2, pid.Name);
        }

        [DataRow("a", "b")]
        [DataRow("default", "a")]
        [DataTestMethod]
        public void TestFromStringTwoArg(string s1, string s2)
        {
            var x = new PodIdentifierFactory();

            var pid = x.FromString(s1, s2);

            Assert.AreEqual(s1, pid.Namespace);
            Assert.AreEqual(s2, pid.Name);
        }

    }
}
