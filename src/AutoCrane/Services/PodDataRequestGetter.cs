// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;

namespace AutoCrane.Services
{
    internal class PodDataRequestGetter : IPodDataRequestGetter
    {
        private readonly KubernetesClient client;

        public PodDataRequestGetter(KubernetesClient client)
        {
            this.client = client;
        }

        public async Task<IReadOnlyList<PodDataRequestInfo>> GetAsync(string ns)
        {
            var pods = await this.client.GetPodAnnotationAsync(ns);
            var result = new List<PodDataRequestInfo>(pods.Count);
            foreach (var pod in pods)
            {
                var dataRequest = new PodDataRequestInfo(pod.Id, pod.Annotations);
                if (dataRequest.ActiveRequests.Any())
                {
                    result.Add(dataRequest);
                }
            }

            return result;
        }
    }
}
