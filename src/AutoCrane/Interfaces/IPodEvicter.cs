// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading.Tasks;
using AutoCrane.Models;

namespace AutoCrane.Interfaces
{
    public interface IPodEvicter
    {
        Task EvictPodAsync(PodIdentifier p);
    }
}
