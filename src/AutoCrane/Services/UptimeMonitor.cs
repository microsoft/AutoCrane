// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using AutoCrane.Interfaces;

namespace AutoCrane.Services
{
    internal sealed class UptimeMonitor : IUptimeMonitor
    {
        private readonly IClock clock;
        private readonly DateTimeOffset start;

        public UptimeMonitor(IClock clock)
        {
            this.clock = clock;
            this.start = clock.Get();
        }

        public TimeSpan Uptime => this.clock.Get() - this.start;
    }
}
