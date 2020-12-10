// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Services
{
    internal sealed class DataRepositoryUpgradeOracleFactory : IDataRepositoryUpgradeOracleFactory
    {
        private readonly ILogger<DataRepositoryUpgradeOracle> logger;
        private readonly IClock clock;

        public DataRepositoryUpgradeOracleFactory(ILoggerFactory loggerFactory, IClock clock)
        {
            this.logger = loggerFactory.CreateLogger<DataRepositoryUpgradeOracle>();
            this.clock = clock;
        }

        public IDataRepositoryUpgradeOracle Create(DataRepositoryKnownGoods knownGoods, DataRepositoryLatestVersionInfo latestVersionInfo, IReadOnlyList<PodDataRequestInfo> pods)
        {
            return new DataRepositoryUpgradeOracle(knownGoods, latestVersionInfo, pods, this.logger, this.clock);
        }

        private class DataRepositoryUpgradeOracle : IDataRepositoryUpgradeOracle
        {
            private readonly DataRepositoryKnownGoods knownGoods;
            private readonly DataRepositoryLatestVersionInfo latestVersionInfo;
            private readonly IReadOnlyList<PodDataRequestInfo> pods;
            private readonly ILogger<DataRepositoryUpgradeOracle> logger;
            private readonly IClock clock;

            public DataRepositoryUpgradeOracle(DataRepositoryKnownGoods knownGoods, DataRepositoryLatestVersionInfo latestVersionInfo, IReadOnlyList<PodDataRequestInfo> pods, ILogger<DataRepositoryUpgradeOracle> logger, IClock clock)
            {
                this.knownGoods = knownGoods;
                this.latestVersionInfo = latestVersionInfo;
                this.pods = pods;
                this.logger = logger;
                this.clock = clock;
            }

            public bool ShouldMakeRequest(string repo, string existingVersion, out string newVersion)
            {
                newVersion = string.Empty;

                // if there isn't an existing version, use the LKG version
                if (string.IsNullOrEmpty(existingVersion))
                {
                    var knownGoodVersion = DataDownloadRequestDetails.FromBase64Json(this.knownGoods.KnownGoodVersions[repo]);
                    if (knownGoodVersion is null)
                    {
                        return false;
                    }

                    knownGoodVersion.UpdateTimestamp(this.clock);
                    newVersion = knownGoodVersion.ToBase64String();
                    return true;
                }

                return false;
            }
        }
    }
}
