using AutoCrane.Models;
using AutoCrane.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace AutoCrane.Tests
{
    [TestClass]
    public class UpgradeOracleTests
    {
        [TestMethod]
        public void TestInitialDataRequestMatchesLastKnownGood()
        {
            var fakeClock = new ManualClock()
            {
                Time = DateTimeOffset.FromUnixTimeSeconds(1_000),
            };

            var f = new DataRepositoryUpgradeOracleFactory(new LoggerFactory(), fakeClock);
            var kg = new DataRepositoryKnownGoods(new Dictionary<string, string>()
            {
                ["d"] = new DataDownloadRequestDetails("a", "a").ToBase64String(),
            });

            var lv = new DataRepositoryLatestVersionInfo(new Dictionary<string, string>()
            {
                ["d"] = new DataDownloadRequestDetails("b", "b").ToBase64String(),
            });

            var pods = new List<PodDataRequestInfo>()
            {
                new PodDataRequestInfo(
                    new PodIdentifier("ns", "name"),
                    new Dictionary<string, string>()
                    {
                        [$"{CommonAnnotations.DataDeploymentPrefix}1"] = "d"
                    }),
            };

            var o = f.Create(kg, lv, pods);

            Assert.IsNull(o.GetDataRequest(pods[0].Id, "0"), "Request to non-existing repo should not return a request");
            AssertSameData(kg.KnownGoodVersions["d"], o.GetDataRequest(pods[0].Id, "1"), $"Initial request should be LKG");
        }

        [TestMethod]
        public void TestDataRequestMatchesLastKnownGood()
        {
            var fakeClock = new ManualClock()
            {
                Time = DateTimeOffset.FromUnixTimeSeconds(1_000),
            };

            var f = new DataRepositoryUpgradeOracleFactory(new LoggerFactory(), fakeClock);
            var kg = new DataRepositoryKnownGoods(new Dictionary<string, string>()
            {
                ["d"] = new DataDownloadRequestDetails("a", "a").ToBase64String(),
            });

            var lv = new DataRepositoryLatestVersionInfo(new Dictionary<string, string>()
            {
                ["d"] = new DataDownloadRequestDetails("a", "a").ToBase64String(),
            });

            var pods = new List<PodDataRequestInfo>()
            {
                new PodDataRequestInfo(
                    new PodIdentifier("ns", "name"),
                    new Dictionary<string, string>()
                    {
                        [$"{CommonAnnotations.DataDeploymentPrefix}1"] = "d",
                        [$"{CommonAnnotations.DataRequestPrefix}1"] = kg.KnownGoodVersions["d"],
                    }),
            };

            var o = f.Create(kg, lv, pods);

            Assert.IsNull(o.GetDataRequest(pods[0].Id, "1"), "Request should return nothing if upgrade would be same as LKG");
        }

        [TestMethod]
        public void TestDataRequestOnePodUpgrade()
        {
            var fakeClock = new ManualClock()
            {
                Time = DateTimeOffset.FromUnixTimeSeconds(1_000),
            };

            var f = new DataRepositoryUpgradeOracleFactory(new LoggerFactory(), fakeClock);
            var kg = new DataRepositoryKnownGoods(new Dictionary<string, string>()
            {
                ["d"] = new DataDownloadRequestDetails("a", "a").ToBase64String(),
            });

            var lv = new DataRepositoryLatestVersionInfo(new Dictionary<string, string>()
            {
                ["d"] = new DataDownloadRequestDetails("b", "b").ToBase64String(),
            });

            var pods = new List<PodDataRequestInfo>()
            {
                new PodDataRequestInfo(
                    new PodIdentifier("ns", "name"),
                    new Dictionary<string, string>()
                    {
                        [$"{CommonAnnotations.DataDeploymentPrefix}1"] = "d",
                        [$"{CommonAnnotations.DataRequestPrefix}1"] = kg.KnownGoodVersions["d"],
                    }),
            };

            var o = f.Create(kg, lv, pods);

            AssertSameData(lv.UpgradeInfo["d"], o.GetDataRequest(pods[0].Id, "1"), $"With one pod, upgrade to latest if on LKG");
        }

        private static void AssertSameData(string expected, DataDownloadRequestDetails? actual, string msg)
        {
            Assert.IsNotNull(actual);
            var expectedData = DataDownloadRequestDetails.FromBase64Json(expected);
            AssertSameData(expectedData, actual, msg);
        }

        private static void AssertSameData(DataDownloadRequestDetails? expectedData, DataDownloadRequestDetails? actualData, string msg)
        {
            Assert.IsNotNull(expectedData);
            Assert.IsNotNull(actualData);
            Assert.AreEqual(expectedData!.Hash, actualData!.Hash, $"DataDownloadRequestDetails.Hash mismatch: {msg}");
            Assert.AreEqual(expectedData.Path, actualData.Path, $"DataDownloadRequestDetails.Path mismatch: {msg}");
        }
    }
}
