// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;

namespace AutoCrane.Services
{
    internal class PodAnnotationPutter : IPodAnnotationPutter
    {
        private readonly KubernetesClient client;

        public PodAnnotationPutter(KubernetesClient client)
        {
            this.client = client;
        }

        public Task PutPodAnnotationAsync(PodIdentifier pod, string name, string val)
        {
            return this.client.PutPodAnnotationAsync(pod, name, val);
        }
    }
}
