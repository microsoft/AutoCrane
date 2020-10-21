// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using AutoCrane.Models;

namespace AutoCrane.Interfaces
{
    public interface IKubernetesClient
    {
        Task PutPodAnnotationAsync(PodIdentifier pod, string name, string val);

        Task<IReadOnlyDictionary<string, string>> GetPodAnnotationAsync(PodIdentifier podIdentifier);

        Task<IReadOnlyList<string>> GetFailingPodsAsync(string ns);

        Task EvictPodAsync(PodIdentifier p);
    }
}
