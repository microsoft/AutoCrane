// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using AutoCrane.Models;

namespace AutoCrane.Interfaces
{
    public interface IDataRepositoryUpgradeOracle
    {
        string? GetDataRequest(PodIdentifier pi, string repo);
    }
}
