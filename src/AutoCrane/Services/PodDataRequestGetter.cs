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
            return pods.Select(p => new PodDataRequestInfo(p.Id, p.Annotations)).ToList();
        }
    }
}
