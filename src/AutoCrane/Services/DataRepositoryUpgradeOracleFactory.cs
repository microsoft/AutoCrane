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
        private static readonly TimeSpan UpgradeProbationTimeSpan = TimeSpan.FromMinutes(10);
        private static readonly double UpgradePercent = 0.25;

        private readonly ILogger<DataRepositoryUpgradeOracle> logger;
        private readonly IClock clock;
        private readonly IWatchdogStatusAggregator watchdogStatusAggregator;

        public DataRepositoryUpgradeOracleFactory(ILoggerFactory loggerFactory, IClock clock, IWatchdogStatusAggregator watchdogStatusAggregator)
        {
            this.logger = loggerFactory.CreateLogger<DataRepositoryUpgradeOracle>();
            this.clock = clock;
            this.watchdogStatusAggregator = watchdogStatusAggregator;
        }

        public IDataRepositoryUpgradeOracle Create(DataRepositoryKnownGoods knownGoods, DataRepositoryLatestVersionInfo latestVersionInfo, IReadOnlyList<PodDataRequestInfo> pods)
        {
            return new DataRepositoryUpgradeOracle(knownGoods, latestVersionInfo, pods, this.logger, this.clock, this.watchdogStatusAggregator);
        }

        private class DataRepositoryUpgradeOracle : IDataRepositoryUpgradeOracle
        {
            private readonly DataRepositoryKnownGoods knownGoods;
            private readonly DataRepositoryLatestVersionInfo latestVersionInfo;
            private readonly IReadOnlyList<PodDataRequestInfo> pods;
            private readonly IClock clock;
            private readonly ILogger<DataRepositoryUpgradeOracle> logger;
            private readonly Dictionary<string, List<PodDataRequestInfo>> podsWithRepo;
            private readonly IWatchdogStatusAggregator watchdogStatusAggregator;
            private readonly Dictionary<string, List<PodDataRequestInfo>> podsDependingOnRepo;

            public DataRepositoryUpgradeOracle(DataRepositoryKnownGoods knownGoods, DataRepositoryLatestVersionInfo latestVersionInfo, IReadOnlyList<PodDataRequestInfo> pods, ILogger<DataRepositoryUpgradeOracle> logger, IClock clock, IWatchdogStatusAggregator watchdogStatusAggregator)
            {
                this.knownGoods = knownGoods;
                this.latestVersionInfo = latestVersionInfo;
                this.pods = pods;
                this.logger = logger;
                this.clock = clock;
                this.watchdogStatusAggregator = watchdogStatusAggregator;

                // this is likely a list of data deployment daemonsets
                this.podsWithRepo = new Dictionary<string, List<PodDataRequestInfo>>();
                foreach (var pod in pods)
                {
                    foreach (var repo in pod.DataRepos)
                    {
                        var repoSpec = repo.Value;
                        if (!this.podsWithRepo.TryGetValue(repoSpec, out var list))
                        {
                            list = new List<PodDataRequestInfo>();
                            this.podsWithRepo[repoSpec] = list;
                        }

                        list.Add(pod);
                    }
                }

                // this is a list of pods mounting volumes to the daemonsets supplying the data
                // we look at their watchdogs
                this.podsDependingOnRepo = new Dictionary<string, List<PodDataRequestInfo>>();
                foreach (var pod in pods)
                {
                    foreach (var repo in pod.DependsOn)
                    {
                        if (!this.podsDependingOnRepo.TryGetValue(repo, out var list))
                        {
                            list = new List<PodDataRequestInfo>();
                            this.podsDependingOnRepo[repo] = list;
                        }

                        list.Add(pod);
                    }
                }
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
                    this.logger.LogError($"{pi} {repoName}/{repoSpec}: LKG missing, cannot set LKG");
                    return null;
                }

                if (!this.latestVersionInfo.UpgradeInfo.TryGetValue(repoSpec, out var repoDetailsLatestVersion))
                {
                    this.logger.LogError($"{pi} {repoName}/{repoSpec}: latest version missing");
                    return null;
                }

                var knownGoodVersion = DataDownloadRequestDetails.FromBase64Json(repoDetailsKnownGoodVersion);
                if (knownGoodVersion is null)
                {
                    this.logger.LogError($"{pi} Cannot parse known good version of data {repoSpec}: {repoDetailsKnownGoodVersion}");
                    return null;
                }

                var latestVersion = DataDownloadRequestDetails.FromBase64Json(repoDetailsLatestVersion);
                if (latestVersion is null)
                {
                    this.logger.LogError($"{pi} Cannot parse known good version of data {repoSpec}: {repoDetailsLatestVersion}");
                    return null;
                }

                if (pod.Requests.TryGetValue(repoName, out var existingVersionString))
                {
                    var existingVersion = DataDownloadRequestDetails.FromBase64Json(existingVersionString);
                    if (existingVersion is null)
                    {
                        // if we can't parse the existing version, try setting it to LKG
                        this.logger.LogError($"{pi} Cannot parse existing version {existingVersionString}, setting to LKG: {knownGoodVersion}");
                        return knownGoodVersion;
                    }

                    if (existingVersion.UnixTimestampSeconds is null)
                    {
                        this.logger.LogError($"{pi} Existing version {existingVersionString}, does not have timestamp, returning LKG: {knownGoodVersion}");
                        return knownGoodVersion;
                    }

                    if (existingVersion.Equals(latestVersion))
                    {
                        this.logger.LogInformation($"{pi} {repoName}/{repoSpec} is on latest version, doing nothing.");
                        return null;
                    }

                    if (knownGoodVersion.Equals(latestVersion))
                    {
                        this.logger.LogInformation($"{pi} {repoName}/{repoSpec} LKG == latest, nothing to do.");
                        return null;
                    }

                    var existingVersionTimestamp = DateTimeOffset.FromUnixTimeSeconds(existingVersion.UnixTimestampSeconds.GetValueOrDefault());
                    if (existingVersionTimestamp > this.clock.Get() - UpgradeProbationTimeSpan)
                    {
                        this.logger.LogInformation($"{pi} {repoName}/{repoSpec} upgraded recently. ignoring");
                        return null;
                    }

                    // we know we aren't on the latest version, if we aren't on LKG, upgrade to latest
                    if (!existingVersion.Equals(knownGoodVersion))
                    {
                        this.logger.LogInformation($"{pi} {repoName}/{repoSpec} is on version between LKG and latest, moving to latest");
                        return latestVersion;
                    }

                    // if FailingLimit% has been on latest version for at least UpgradeProbationTimeSpan, and there are no watchdog failures on dependent users, then
                    // set LKG to latest, upgrade everyone to latest
                    var podsUsingThisData = this.podsWithRepo[repoSpec];
                    if (this.podsDependingOnRepo.TryGetValue(repoSpec, out var podsDependingOnThisData))
                    {
                        var watchdogFailureCount = podsDependingOnThisData.Where(p => this.watchdogStatusAggregator.Aggregate(p.Annotations) == WatchdogStatus.ErrorLevel).Count();
                        var pctFailing = (double)watchdogFailureCount / podsDependingOnThisData.Count;
                        if (pctFailing > UpgradePercent)
                        {
                            this.logger.LogInformation($"{pi} {repoName}/{repoSpec} found watchdog failures on pods {watchdogFailureCount}/{podsDependingOnThisData.Count}, taking no action");
                            return null;
                        }
                    }

                    var podsOnLatestVersionForProbationTimeSpanCount = podsUsingThisData.Where(p => this.PodIsOnVersionForAtLeast(p, repoSpec, latestVersion, UpgradeProbationTimeSpan)).Count();
                    var podsOnLatestVersionForProbation = (double)podsOnLatestVersionForProbationTimeSpanCount / podsUsingThisData.Count;
                    if (podsOnLatestVersionForProbation >= UpgradePercent)
                    {
                        this.logger.LogInformation($"{pi} {repoName}/{repoSpec} found pods {podsOnLatestVersionForProbationTimeSpanCount}/{podsUsingThisData.Count} on latest for probation period, upgrading to latest {latestVersion}");
                        return latestVersion;
                    }

                    // otherwise put FailingLimit% on Latest and the rest on LKG
                    var podsOnLKG = podsUsingThisData.Where(p => this.PodIsOnVersionForAtLeast(p, repoSpec, knownGoodVersion, null)).ToList();

                    // round down or we might never upgrade anyone
                    var numberToTake = (int)(Math.Floor(1.0 - UpgradePercent) * podsUsingThisData.Count);
                    var shouldNotUpgradeList = podsOnLKG.Take(numberToTake).Select(p => p.Id).ToHashSet();
                    if (shouldNotUpgradeList.Contains(pod.Id))
                    {
                        this.logger.LogInformation($"{pi} {repoName}/{repoSpec} in do not upgrade list");
                        return null;
                    }

                    // put on latest version
                    this.logger.LogInformation($"{pi} {repoName}/{repoSpec} upgrading to: {latestVersion}");
                    return latestVersion;
                }
                else
                {
                    // doesn't have a request set, so default to LKG
                    this.logger.LogInformation($"{pi} {repoName}/{repoSpec} has no request, setting to LKG: {knownGoodVersion}");
                    return knownGoodVersion;
                }
            }

            private bool PodIsOnVersionForAtLeast(PodDataRequestInfo p, string repoSpec, DataDownloadRequestDetails ver, TimeSpan? timespan)
            {
                var requestKey = p.DataRepos.FirstOrDefault(r => r.Value == repoSpec).Key;
                var request = p.Requests.FirstOrDefault(r => r.Key == requestKey).Value;
                if (request is not null)
                {
                    var version = DataDownloadRequestDetails.FromBase64Json(request);
                    if (version != null && version.Equals(ver))
                    {
                        if (!timespan.HasValue)
                        {
                            return true;
                        }
                        else
                        {
                            return version.UnixTimestampSeconds > (this.clock.Get() - timespan.Value).ToUnixTimeSeconds();
                        }
                    }
                }

                return false;
            }
        }
    }
}
