// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Options;

namespace AutoCrane.Services
{
    internal sealed class ConsecutiveHealthMonitor : IConsecutiveHealthMonitor
    {
        private readonly IClock clock;
        private readonly IWatchdogStatusGetter watchdogStatusGetter;
        private readonly ConcurrentDictionary<PodIdentifier, DateTimeOffset> firstHealthyProbe;
        private readonly TimeSpan minTimeHealthy;

        public ConsecutiveHealthMonitor(IClock clock, IWatchdogStatusGetter watchdogStatusGetter, IOptions<WatchdogHealthzOptions> options)
        {
            this.clock = clock;
            this.watchdogStatusGetter = watchdogStatusGetter;
            this.firstHealthyProbe = new ConcurrentDictionary<PodIdentifier, DateTimeOffset>();
            this.minTimeHealthy = TimeSpan.FromSeconds(options.Value.RequireHealthyStatusForSeconds.GetValueOrDefault());
        }

        public bool IsHealthy(PodIdentifier podid)
        {
            if (this.firstHealthyProbe.TryGetValue(podid, out var firstHealthy))
            {
                var healthyFor = this.clock.Get() - firstHealthy;
                return healthyFor > this.minTimeHealthy;
            }
            else
            {
                // we don't have any recorded status in the database, so assume not healthy
                return false;
            }
        }

        public async Task Probe(PodIdentifier id)
        {
            var status = await this.watchdogStatusGetter.GetStatusAsync(id);
            bool isFailure = status.Any(s => s.IsFailure);
            if (isFailure)
            {
                this.firstHealthyProbe.AddOrUpdate(id, DateTimeOffset.MinValue, (_, old) => DateTimeOffset.MinValue);
            }
            else
            {
                this.firstHealthyProbe.AddOrUpdate(id, this.clock.Get(), (_, old) => old);
            }
        }
    }
}
