// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading.Tasks;
using AutoCrane.Models;

namespace AutoCrane.Interfaces
{
    public interface IPodAnnotationPutter
    {
        Task PutPodAnnotationAsync(PodIdentifier pod, string name, string val);
    }
}
