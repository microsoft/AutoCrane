// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
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

            public DataDownloadRequestDetails? GetDataRequest(PodIdentifier pi, string repoName)
            {
                var pod = this.pods.FirstOrDefault(p => p.Id == pi);
                if (pod == null)
                {
                    this.logger.LogError($"Cannot find pod {pi}, so cannot determine correct data request");
                    return null;
                }

                // there is no existing request
                if (!pod.DataRepos.TryGetValue(repoName, out var repoSpec))
                {
                    // we can't even find the data source, bailout--this shouldn't happen
                    this.logger.LogError($"Pod {pod.Id} has data request {repoName} but could not find data source.");
                    return null;
                }

                // try parsing the LKG and latest values
                if (!this.knownGoods.KnownGoodVersions.TryGetValue(repoSpec, out var repoDetailsKnownGoodVersion))
                {
                    this.logger.LogError($"{repoName}/{repoSpec}: LKG missing, cannot set LKG");
                    return null;
                }

                if (!this.latestVersionInfo.UpgradeInfo.TryGetValue(repoSpec, out var repoDetailsLatestVersion))
                {
                    this.logger.LogError($"{repoName}/{repoSpec}: latest version missing");
                    return null;
                }

                var knownGoodVersion = DataDownloadRequestDetails.FromBase64Json(repoDetailsKnownGoodVersion);
                if (knownGoodVersion is null)
                {
                    this.logger.LogError($"Cannot parse known good version of data {repoSpec}: {repoDetailsKnownGoodVersion}");
                    return null;
                }

                var latestVersion = DataDownloadRequestDetails.FromBase64Json(repoDetailsLatestVersion);
                if (latestVersion is null)
                {
                    this.logger.LogError($"Cannot parse known good version of data {repoSpec}: {repoDetailsLatestVersion}");
                    return null;
                }

                if (pod.Requests.TryGetValue(repoName, out var existingVersionString))
                {
                    var existingVersion = DataDownloadRequestDetails.FromBase64Json(existingVersionString);
                    if (existingVersion is null)
                    {
                        // if we can't parse the existing version, try setting it to LKG
                        return knownGoodVersion;
                    }

                    if (existingVersion.Equals(latestVersion))
                    {
                        return null;
                    }

                    // unchecked upgrade logic
                    return latestVersion;
                }
                else
                {
                    // doesn't have a request set, so default to LKG
                    return knownGoodVersion;
                }
            }
        }
    }
}
