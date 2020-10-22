// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using AutoCrane.Models;

namespace AutoCrane.Interfaces
{
    public interface IPodAnnotationGetter
    {
        Task<IReadOnlyList<PodWithAnnotations>> GetPodAnnotationAsync(string ns);

        Task<PodWithAnnotations> GetPodAnnotationAsync(PodIdentifier podIdentifier);
    }
}
