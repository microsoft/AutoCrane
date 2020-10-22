// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;

namespace AutoCrane.Services
{
    internal class PodEvicter : IPodEvicter
    {
        private readonly KubernetesClient client;

        public PodEvicter(KubernetesClient client)
        {
            this.client = client;
        }

        public Task EvictPodAsync(PodIdentifier p)
        {
            return this.client.EvictPodAsync(p);
        }
    }
}
