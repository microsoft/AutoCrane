// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using AutoCrane.Interfaces;

namespace AutoCrane.Services
{
    internal sealed class ServiceHeartbeat : IServiceHeartbeat
    {
        private readonly IClock clock;
        private readonly ConcurrentDictionary<string, DateTimeOffset> lastBeats;

        public ServiceHeartbeat(IClock clock)
        {
            this.clock = clock;
            this.lastBeats = new ConcurrentDictionary<string, DateTimeOffset>();
        }

        public void Beat(string name)
        {
            this.lastBeats.AddOrUpdate(name, (_) => this.clock.Get(), (_, _) => this.clock.Get());
        }

        public TimeSpan? GetLastBeat(string name)
        {
            if (this.lastBeats.TryGetValue(name, out var val))
            {
                return this.clock.Get() - val;
            }

            return null;
        }
    }
}
