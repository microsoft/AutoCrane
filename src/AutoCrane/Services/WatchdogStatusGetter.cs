// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;

namespace AutoCrane.Services
{
    internal sealed class WatchdogStatusGetter : IWatchdogStatusGetter
    {
        private readonly KubernetesClient client;

        public WatchdogStatusGetter(KubernetesClient client)
        {
            this.client = client;
        }

        public async Task<IReadOnlyList<WatchdogStatus>> GetStatusAsync(PodIdentifier podIdentifier)
        {
            var pod = await this.client.GetPodAnnotationAsync(podIdentifier);
            var list = new List<WatchdogStatus>();
            foreach (var annotation in pod.Annotations)
            {
                if (!annotation.Key.StartsWith(WatchdogStatus.Prefix))
                {
                    continue;
                }

                var splits = annotation.Value.Split('/', 2);
                if (splits.Length > 1)
                {
                    list.Add(new WatchdogStatus()
                    {
                        Name = annotation.Key[WatchdogStatus.Prefix.Length..],
                        Level = splits[0],
                        Message = splits[1],
                    });
                }
            }

            return list;
        }
    }
}
