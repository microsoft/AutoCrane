// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using AutoCrane.Models;

namespace AutoCrane.Interfaces
{
    public interface IDataRepositoryUpgradeOracleFactory
    {
        IDataRepositoryUpgradeOracle Create(DataRepositoryKnownGoods knownGoods, DataRepositoryLatestVersionInfo latestVersionInfo, IReadOnlyList<PodDataRequestInfo> pods);
    }
}
