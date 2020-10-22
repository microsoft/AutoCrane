// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;

namespace AutoCrane.Services
{
    internal class PodAnnotationGetter : IPodAnnotationGetter
    {
        private readonly KubernetesClient client;

        public PodAnnotationGetter(KubernetesClient client)
        {
            this.client = client;
        }

        public Task<IReadOnlyList<PodWithAnnotations>> GetPodAnnotationAsync(string ns)
        {
            return this.client.GetPodAnnotationAsync(ns);
        }

        public Task<PodWithAnnotations> GetPodAnnotationAsync(PodIdentifier podIdentifier)
        {
            return this.client.GetPodAnnotationAsync(podIdentifier);
        }
    }
}
