// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Options;

namespace AutoCrane.Services
{
    internal class PodAnnotationPutter : IPodAnnotationPutter
    {
        private readonly KubernetesClient client;
        private readonly IOptions<PodIdentifierOptions> thisPodOptions;

        public PodAnnotationPutter(KubernetesClient client, IOptions<PodIdentifierOptions> thisPodOptions)
        {
            this.client = client;
            this.thisPodOptions = thisPodOptions;
        }

        public Task PutPodAnnotationAsync(string name, string val)
        {
            return this.client.PutPodAnnotationAsync(this.thisPodOptions.Value.Identifier, name, val);
        }

        public Task PutPodAnnotationAsync(PodIdentifier pod, string name, string val)
        {
            return this.client.PutPodAnnotationAsync(pod, name, val);
        }

        public Task PutPodAnnotationAsync(PodIdentifier pod, IReadOnlyList<KeyValuePair<string, string>> annotations)
        {
            return this.client.PutPodAnnotationAsync(pod, annotations);
        }
    }
}
