using System.Collections.Generic;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;

namespace AutoCrane.Tests
{
    internal class FakeWatchdogStatusGetter : IWatchdogStatusGetter
    {
        public IReadOnlyList<WatchdogStatus> Result { get; set; } = new List<WatchdogStatus>();

        public Task<IReadOnlyList<WatchdogStatus>> GetStatusAsync(PodIdentifier pod)
        {
            return Task.FromResult(this.Result);
        }
    }
}
