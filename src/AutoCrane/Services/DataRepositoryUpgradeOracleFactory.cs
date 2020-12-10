// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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

            public string? GetDataRequest(PodIdentifier pi, string repoName)
            {
                var pod = this.pods.FirstOrDefault(p => p.Id == pi);
                if (pod == null)
                {
                    this.logger.LogError($"Cannot find pod {pi}, so cannot determine correct data request");
                    return null;
                }

                if (pod.Requests.TryGetValue(repoName, out var existingVersion))
                {
                    return null;
                }
                else
                {
                    // there is no existing request
                    if (!pod.DataRepos.TryGetValue(repoName, out var repoSpec))
                    {
                        // we can't even find the data source, bailout--this shouldn't happen
                        this.logger.LogError($"Pod {pi} has data request {repoName} but could not find data source.");
                        return null;
                    }

                    if (this.knownGoods.KnownGoodVersions.TryGetValue(repoSpec, out var repoDetails))
                    {
                        var knownGoodVersion = DataDownloadRequestDetails.FromBase64Json(repoDetails);
                        if (knownGoodVersion is null)
                        {
                            this.logger.LogError($"Cannot parse known good version of data {repoSpec}: {repoDetails}");
                            return null;
                        }

                        knownGoodVersion.UpdateTimestamp(this.clock);
                        return knownGoodVersion.ToBase64String();
                    }
                    else
                    {
                        this.logger.LogError($"{repoName}/{repoSpec}: LKG missing, cannot set LKG");
                        return null;
                    }
                }
            }
        }
    }
}
