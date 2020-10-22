// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;

namespace AutoCrane.Services
{
    internal class PodGetter : IPodGetter
    {
        private readonly KubernetesClient client;

        public PodGetter(KubernetesClient client)
        {
            this.client = client;
        }

        public Task<IReadOnlyList<PodInfo>> GetPodsAsync(string ns)
        {
            return this.client.GetPodAnnotationAsync(ns);
        }

        public Task<PodInfo> GetPodAsync(PodIdentifier podIdentifier)
        {
            return this.client.GetPodAnnotationAsync(podIdentifier);
        }
    }
}
