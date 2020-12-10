// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using AutoCrane.Models;

namespace AutoCrane.Interfaces
{
    public interface IDataRepositoryUpgradeOracle
    {
        DataDownloadRequestDetails? GetDataRequest(PodIdentifier pi, string repo);
    }
}
