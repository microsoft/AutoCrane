// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;

namespace AutoCrane.Services
{
    internal class FailingPodGetter : IFailingPodGetter
    {
        private readonly KubernetesClient client;

        public FailingPodGetter(KubernetesClient client)
        {
            this.client = client;
        }

        public Task<IReadOnlyList<PodIdentifier>> GetFailingPodsAsync(string ns)
        {
            return this.client.GetFailingPodsAsync(ns);
        }
    }
}
