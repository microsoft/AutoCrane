using AutoCrane.Interfaces;
using AutoCrane.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AutoCrane.Tests
{
    [TestClass]
    public class LeaderElectionTests
    {
        [TestMethod]
        public async Task TestInit()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            IReadOnlyDictionary<string, string>? output = null;
            var x = Setup(() => output ?? new Dictionary<string, string>(), (string x, string y, IReadOnlyDictionary<string, string> props, CancellationToken ct) => output = props);

            await x.StartBackgroundTask("blah", TimeSpan.FromSeconds(2), cts.Token);

            Assert.IsNotNull(output);
            Assert.IsTrue(output!.ContainsKey("control-plane.alpha.kubernetes.io/leader"));
            var record = JsonSerializer.Deserialize<LeaderElection.LeaderElectionRecord>(output!["control-plane.alpha.kubernetes.io/leader"]);
            Assert.AreEqual(Environment.MachineName, record!.HolderIdentity);
            Assert.AreEqual(0, record.LeaderTransitions);
            Assert.IsTrue(DateTime.Now - record.AcquireTime! < TimeSpan.FromSeconds(30));
            Assert.IsTrue(DateTime.Now - record.RenewTime! < TimeSpan.FromSeconds(30));
        }

        [TestMethod]
        public async Task TestTakeover()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var oldRecord = new LeaderElection.LeaderElectionRecord()
            {
                HolderIdentity = "xyz",
                AcquireTime = DateTime.Now - TimeSpan.FromDays(5),
                RenewTime = DateTime.Now - TimeSpan.FromDays(5),
                LeaderTransitions = 5,
            };

            IReadOnlyDictionary<string, string> output = new Dictionary<string, string>() { ["control-plane.alpha.kubernetes.io/leader"] = JsonSerializer.Serialize(oldRecord) };

            var x = Setup(() => output, (string x, string y, IReadOnlyDictionary<string, string> props, CancellationToken ct) => output = props);

            await x.StartBackgroundTask("blah", TimeSpan.FromSeconds(2), cts.Token);

            Assert.IsNotNull(output);
            Assert.IsTrue(output!.ContainsKey("control-plane.alpha.kubernetes.io/leader"));
            var record = JsonSerializer.Deserialize<LeaderElection.LeaderElectionRecord>(output!["control-plane.alpha.kubernetes.io/leader"]);
            Assert.AreEqual(Environment.MachineName, record!.HolderIdentity);
            Assert.AreEqual(6, record.LeaderTransitions);
            Assert.IsTrue(DateTime.Now - record.AcquireTime! < TimeSpan.FromSeconds(30));
            Assert.IsTrue(DateTime.Now - record.RenewTime! < TimeSpan.FromSeconds(30));
        }

        private ILeaderElection Setup(Func<IReadOnlyDictionary<string, string>> getAnnotations, Action<string, string, IReadOnlyDictionary<string, string>, CancellationToken> putAnnotations)
        {
            var epa = new Mock<IEndpointAnnotationAccessor>();
            epa.Setup(m => m.GetEndpointAnnotationsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(getAnnotations);
            epa.Setup(m => m.PutEndpointAnnotationsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>())).Callback(putAnnotations);
            var config = new Mock<IAutoCraneConfig>();
            config.Setup(m => m.Namespaces).Returns(new string[] { "ns" });
            config.Setup(m => m.IsAllowedNamespace(It.IsAny<string>())).Returns(true);

            var le = new LeaderElection(epa.Object, config.Object, new LoggerFactory());
            return le;
        }
    }
}
