// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;

namespace AutoCrane.Services
{
    internal sealed class WatchdogStatusPutter : IWatchdogStatusPutter
    {
        private readonly KubernetesClient client;
        private readonly IClock clock;

        public WatchdogStatusPutter(KubernetesClient client, IClock clock)
        {
            this.client = client;
            this.clock = clock;
        }

        public Task PutStatusAsync(PodIdentifier pod, WatchdogStatus status)
        {
            return this.PutStatusAsync(pod, new List<WatchdogStatus>() { status });
        }

        public Task PutStatusAsync(PodIdentifier pod, IReadOnlyList<WatchdogStatus> statusList)
        {
            return this.client.PutPodAnnotationAsync(
                pod,
                statusList.Select(status =>
                    new KeyValuePair<string, string>($"{WatchdogStatus.Prefix}{status.Name}", $"{status.Level!.ToLowerInvariant()}/{this.clock.Get():s}/{status.Message}")).ToArray());
        }
    }
}
