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

            var f = new DataRepositoryUpgradeOracleFactory(new LoggerFactory(), fakeClock, new WatchdogStatusAggregator());
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
        public void RequestMatchesLastKnownGood()
        {
            var fakeClock = new ManualClock()
            {
                Time = DateTimeOffset.FromUnixTimeSeconds(1_000),
            };

            var f = new DataRepositoryUpgradeOracleFactory(new LoggerFactory(), fakeClock, new WatchdogStatusAggregator());
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
        public void RequestRecentUpgrade()
        {
            var fakeClock = new ManualClock()
            {
                Time = DateTimeOffset.UtcNow,
            };

            var f = new DataRepositoryUpgradeOracleFactory(new LoggerFactory(), fakeClock, new WatchdogStatusAggregator());
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

            Assert.IsNull(o.GetDataRequest(pods[0].Id, "1"), $"Should not give a new request because still in probation");
        }

        [TestMethod]
        public void RequestOnePodUpgrade()
        {
            var fakeClock = new ManualClock()
            {
                Time = DateTimeOffset.UtcNow + TimeSpan.FromDays(1),
            };

            var f = new DataRepositoryUpgradeOracleFactory(new LoggerFactory(), fakeClock, new WatchdogStatusAggregator());
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

        [TestMethod]
        public void RequestResetUnparsableToLKG()
        {
            var fakeClock = new ManualClock()
            {
                Time = DateTimeOffset.UtcNow + TimeSpan.FromDays(1),
            };

            var f = new DataRepositoryUpgradeOracleFactory(new LoggerFactory(), fakeClock, new WatchdogStatusAggregator());
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
                        [$"{CommonAnnotations.DataRequestPrefix}1"] = "unparsable",
                    }),
            };

            var o = f.Create(kg, lv, pods);

            AssertSameData(kg.KnownGoodVersions["d"], o.GetDataRequest(pods[0].Id, "1"), $"Reset to LKG on Parse error");
        }

        [TestMethod]
        public void RequestResetNoTimestampVersionToLKG()
        {
            var fakeClock = new ManualClock()
            {
                Time = DateTimeOffset.UtcNow + TimeSpan.FromDays(1),
            };

            var f = new DataRepositoryUpgradeOracleFactory(new LoggerFactory(), fakeClock, new WatchdogStatusAggregator());
            var kg = new DataRepositoryKnownGoods(new Dictionary<string, string>()
            {
                ["d"] = new DataDownloadRequestDetails("a", "a").ToBase64String(),
            });

            var lv = new DataRepositoryLatestVersionInfo(new Dictionary<string, string>()
            {
                ["d"] = new DataDownloadRequestDetails("b", "b").ToBase64String(),
            });

            var badReq = new DataDownloadRequestDetails("b", "b");
            badReq.UnixTimestampSeconds = null;

            var pods = new List<PodDataRequestInfo>()
            {
                new PodDataRequestInfo(
                    new PodIdentifier("ns", "name"),
                    new Dictionary<string, string>()
                    {
                        [$"{CommonAnnotations.DataDeploymentPrefix}1"] = "d",
                        [$"{CommonAnnotations.DataRequestPrefix}1"] = badReq.ToBase64String(),
                    }),
            };

            var o = f.Create(kg, lv, pods);

            AssertSameData(kg.KnownGoodVersions["d"], o.GetDataRequest(pods[0].Id, "1"), $"Reset to LKG on bad timestamp");
        }


        [TestMethod]
        public void RequestUpgradeMiddleVersionToLatest()
        {
            var fakeClock = new ManualClock()
            {
                Time = DateTimeOffset.UtcNow + TimeSpan.FromDays(1),
            };

            var f = new DataRepositoryUpgradeOracleFactory(new LoggerFactory(), fakeClock, new WatchdogStatusAggregator());
            var kg = new DataRepositoryKnownGoods(new Dictionary<string, string>()
            {
                ["d"] = new DataDownloadRequestDetails("a", "a").ToBase64String(),
            });

            var lv = new DataRepositoryLatestVersionInfo(new Dictionary<string, string>()
            {
                ["d"] = new DataDownloadRequestDetails("b", "b").ToBase64String(),
            });

            var middle = new DataDownloadRequestDetails("c", "c");

            var pods = new List<PodDataRequestInfo>()
            {
                new PodDataRequestInfo(
                    new PodIdentifier("ns", "name"),
                    new Dictionary<string, string>()
                    {
                        [$"{CommonAnnotations.DataDeploymentPrefix}1"] = "d",
                        [$"{CommonAnnotations.DataRequestPrefix}1"] = middle.ToBase64String(),
                    }),
            };

            var o = f.Create(kg, lv, pods);

            AssertSameData(lv.UpgradeInfo["d"], o.GetDataRequest(pods[0].Id, "1"), "upgrade to latest");
        }

        [TestMethod]
        public void RequestNoLkg()
        {
            var fakeClock = new ManualClock()
            {
                Time = DateTimeOffset.UtcNow + TimeSpan.FromDays(1),
            };

            var f = new DataRepositoryUpgradeOracleFactory(new LoggerFactory(), fakeClock, new WatchdogStatusAggregator());
            var kg = new DataRepositoryKnownGoods(new Dictionary<string, string>()
            {
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
                        [$"{CommonAnnotations.DataRequestPrefix}1"] = lv.UpgradeInfo["d"],
                    }),
            };

            var o = f.Create(kg, lv, pods);

            Assert.IsNull(o.GetDataRequest(pods[0].Id, "1"));
        }

        [TestMethod]
        public void RequestNoLatest()
        {
            var fakeClock = new ManualClock()
            {
                Time = DateTimeOffset.UtcNow + TimeSpan.FromDays(1),
            };

            var f = new DataRepositoryUpgradeOracleFactory(new LoggerFactory(), fakeClock, new WatchdogStatusAggregator());
            var kg = new DataRepositoryKnownGoods(new Dictionary<string, string>()
            {
                ["d"] = new DataDownloadRequestDetails("b", "b").ToBase64String(),
            });

            var lv = new DataRepositoryLatestVersionInfo(new Dictionary<string, string>()
            {
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

            Assert.IsNull(o.GetDataRequest(pods[0].Id, "1"));
        }


        [TestMethod]
        public void WatchdogsFailingDoNotUpgrade()
        {
            var fakeClock = new ManualClock()
            {
                Time = DateTimeOffset.UtcNow + TimeSpan.FromDays(1),
            };

            var f = new DataRepositoryUpgradeOracleFactory(new LoggerFactory(), fakeClock, new WatchdogStatusAggregator());
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
                    new PodIdentifier("ns", "pod1"),
                    new Dictionary<string, string>()
                    {
                        [$"{CommonAnnotations.DataDeploymentPrefix}1"] = "d",
                        [$"{CommonAnnotations.DataRequestPrefix}1"] = new DataDownloadRequestDetails("a", "a").ToBase64String(),
                    }),
                new PodDataRequestInfo(
                    new PodIdentifier("ns", "pod2"),
                    new Dictionary<string, string>()
                    {
                        [$"{CommonAnnotations.DataDeploymentPrefix}1"] = "d",
                        [$"{CommonAnnotations.DataRequestPrefix}1"] = new DataDownloadRequestDetails("a", "a").ToBase64String(),
                    }),
                new PodDataRequestInfo(
                    new PodIdentifier("ns", "pod3"),
                    new Dictionary<string, string>()
                    {
                        [$"{CommonAnnotations.DataDeploymentPrefix}1"] = "d",
                        [$"{CommonAnnotations.DataRequestPrefix}1"] = new DataDownloadRequestDetails("a", "a").ToBase64String(),
                    }),
                new PodDataRequestInfo(
                    new PodIdentifier("ns", "pod4"),
                    new Dictionary<string, string>()
                    {
                        [$"{CommonAnnotations.DataDependsOn}"] = "d, e, f",
                        [$"{WatchdogStatus.Prefix}x"] = $"{WatchdogStatus.ErrorLevel}/ohno",
                    }),

            };

            var o = f.Create(kg, lv, pods);


            Assert.IsNull(o.GetDataRequest(pods[0].Id, "1"), "watchdog failure on pod 4");
            Assert.IsNull(o.GetDataRequest(pods[1].Id, "1"), "watchdog failure on pod 4");
            Assert.IsNull(o.GetDataRequest(pods[2].Id, "1"), "watchdog failure on pod 4");
        }


        [TestMethod]
        public void PutSomePodsOnLatest()
        {
            var fakeClock = new ManualClock()
            {
                Time = DateTimeOffset.UtcNow + TimeSpan.FromDays(1),
            };

            var f = new DataRepositoryUpgradeOracleFactory(new LoggerFactory(), fakeClock, new WatchdogStatusAggregator());
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
                    new PodIdentifier("ns", "pod1"),
                    new Dictionary<string, string>()
                    {
                        [$"{CommonAnnotations.DataDeploymentPrefix}1"] = "d",
                        [$"{CommonAnnotations.DataRequestPrefix}1"] = new DataDownloadRequestDetails("a", "a").ToBase64String(),
                    }),
                new PodDataRequestInfo(
                    new PodIdentifier("ns", "pod2"),
                    new Dictionary<string, string>()
                    {
                        [$"{CommonAnnotations.DataDeploymentPrefix}1"] = "d",
                        [$"{CommonAnnotations.DataRequestPrefix}1"] = new DataDownloadRequestDetails("a", "a").ToBase64String(),
                    }),
                new PodDataRequestInfo(
                    new PodIdentifier("ns", "pod3"),
                    new Dictionary<string, string>()
                    {
                        [$"{CommonAnnotations.DataDeploymentPrefix}1"] = "d",
                        [$"{CommonAnnotations.DataRequestPrefix}1"] = new DataDownloadRequestDetails("a", "a").ToBase64String(),
                    }),
            };

            var o = f.Create(kg, lv, pods);

            // upgrade 1 to latest
            var pod1 = o.GetDataRequest(pods[0].Id, "1");
            var pod2 = o.GetDataRequest(pods[1].Id, "1");
            var pod3 = o.GetDataRequest(pods[2].Id, "1");
            Assert.IsNull(pod1, "pod should not upgrade");
            Assert.IsNull(pod2, "pod should not upgrade");
            AssertSameData(lv.UpgradeInfo["d"], pod3, "upgrade to latest");
        }

        [TestMethod]
        public void FinishUpgradeToLatest()
        {
            var fakeClock = new ManualClock()
            {
                Time = DateTimeOffset.UtcNow + TimeSpan.FromDays(1),
            };

            var f = new DataRepositoryUpgradeOracleFactory(new LoggerFactory(), fakeClock, new WatchdogStatusAggregator());
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
                    new PodIdentifier("ns", "pod1"),
                    new Dictionary<string, string>()
                    {
                        [$"{CommonAnnotations.DataDeploymentPrefix}1"] = "d",
                        [$"{CommonAnnotations.DataRequestPrefix}1"] = new DataDownloadRequestDetails("a", "a").ToBase64String(),
                    }),
                new PodDataRequestInfo(
                    new PodIdentifier("ns", "pod2"),
                    new Dictionary<string, string>()
                    {
                        [$"{CommonAnnotations.DataDeploymentPrefix}1"] = "d",
                        [$"{CommonAnnotations.DataRequestPrefix}1"] = new DataDownloadRequestDetails("a", "a").ToBase64String(),
                    }),
                new PodDataRequestInfo(
                    new PodIdentifier("ns", "pod3"),
                    new Dictionary<string, string>()
                    {
                        [$"{CommonAnnotations.DataDeploymentPrefix}1"] = "d",
                        [$"{CommonAnnotations.DataRequestPrefix}1"] = new DataDownloadRequestDetails("b", "b").ToBase64String(),
                    }),
            };

            var o = f.Create(kg, lv, pods);

            // upgrade 1 to latest
            var pod1 = o.GetDataRequest(pods[0].Id, "1");
            var pod2 = o.GetDataRequest(pods[1].Id, "1");
            var pod3 = o.GetDataRequest(pods[2].Id, "1");
            AssertSameData(lv.UpgradeInfo["d"], pod1, "upgrade to latest");
            AssertSameData(lv.UpgradeInfo["d"], pod2, "upgrade to latest");
            Assert.IsNull(pod3, "pod already on latest");
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
