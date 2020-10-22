// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using AutoCrane.Models;

namespace AutoCrane.Interfaces
{
    public interface IFailingPodGetter
    {
        Task<IReadOnlyList<PodIdentifier>> GetFailingPodsAsync(string ns);
    }
}
