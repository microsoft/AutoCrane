using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoCrane.Models;
using AutoCrane.Services;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoCrane.Tests
{
    [TestClass]
    public class ConsecutiveHealthMonitorTests
    {
        [TestMethod]
        public async Task TestInitialFail()
        {
            var requireSeconds = 20;
            var ctx = CreateTestContext();
            ctx.Config = Options.Create(new WatchdogHealthzOptions() { MinReadySeconds = requireSeconds });
            var chm = new ConsecutiveHealthMonitor(ctx.Clock, ctx.WatchdogStatusGetter, ctx.Config);

            var pi = new PodIdentifier("a", "b");
            var pi2 = new PodIdentifier("a", "b2");

            Assert.IsFalse(chm.IsHealthy(pi));
            Assert.IsFalse(chm.IsHealthy(pi2));

            ctx.Clock.Time = DateTimeOffset.FromUnixTimeSeconds(100);
            ctx.WatchdogStatusGetter.Result = new List<WatchdogStatus>();
            await chm.Probe(pi);

            Assert.IsFalse(chm.IsHealthy(pi));
            Assert.IsFalse(chm.IsHealthy(pi2));

            // after no failed probes for requireSeconds, we are healthy
            ctx.Clock.Time = ctx.Clock.Time.AddSeconds(requireSeconds + 2);
            Assert.IsTrue(chm.IsHealthy(pi));
            Assert.IsFalse(chm.IsHealthy(pi2));

            // we stay healthy unless there is a bad probe
            ctx.Clock.Time = ctx.Clock.Time.AddYears(10);
            Assert.IsTrue(chm.IsHealthy(pi));
            Assert.IsFalse(chm.IsHealthy(pi2));

            // if there is a failed probe, go back to unhealthy
            ctx.Clock.Time = ctx.Clock.Time.AddSeconds(requireSeconds + 2);
            ctx.WatchdogStatusGetter.Result = new List<WatchdogStatus>()
            {
                new WatchdogStatus() { Level = WatchdogStatus.ErrorLevel },
            };

            await chm.Probe(pi);
            Assert.IsFalse(chm.IsHealthy(pi));
            Assert.IsFalse(chm.IsHealthy(pi2));
        }


        private static TestContext CreateTestContext()
        {
            var ctx = new TestContext();
            return ctx;
        }

        private class TestContext
        {
            public ManualClock Clock { get; set; } = new ManualClock();

            public FakeWatchdogStatusGetter WatchdogStatusGetter { get; set; } = new FakeWatchdogStatusGetter();

            public IOptions<WatchdogHealthzOptions> Config { get; set; } = Options.Create(new WatchdogHealthzOptions());
        }
    }
}
