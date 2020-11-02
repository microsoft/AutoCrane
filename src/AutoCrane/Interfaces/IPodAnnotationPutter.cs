// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using AutoCrane.Models;

namespace AutoCrane.Interfaces
{
    public interface IPodAnnotationPutter
    {
        Task PutPodAnnotationAsync(string name, string val);

        Task PutPodAnnotationAsync(PodIdentifier pod, string name, string val);

        Task PutPodAnnotationAsync(PodIdentifier pod, IReadOnlyList<KeyValuePair<string, string>> annotations);
    }
}
