// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace AutoCrane.Models
{
    public sealed class DataRepositoryLatestVersionInfo
    {
        public DataRepositoryLatestVersionInfo(IReadOnlyDictionary<string, string> upgradeInfo)
        {
            this.UpgradeInfo = upgradeInfo;
        }

        public IReadOnlyDictionary<string, string> UpgradeInfo { get; }
    }
}
