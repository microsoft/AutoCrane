// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace AutoCrane.Interfaces
{
    public interface IDataRepositoryUpgradeOracle
    {
        bool ShouldMakeRequest(string repo, string existingVersion, out string newVersion);
    }
}
