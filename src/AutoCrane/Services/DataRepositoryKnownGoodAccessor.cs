// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Ocsp;

namespace AutoCrane.Services
{
    internal sealed class DataRepositoryKnownGoodAccessor : IDataRepositoryKnownGoodAccessor
    {
        private const string AutoCraneLastKnownGoodEndpointName = "autocranelkg";
        private readonly ILogger<DataRepositoryKnownGoodAccessor> logger;
        private readonly KubernetesClient client;

        public DataRepositoryKnownGoodAccessor(ILoggerFactory loggerFactory, KubernetesClient client)
        {
            this.logger = loggerFactory.CreateLogger<DataRepositoryKnownGoodAccessor>();
            this.client = client;
        }

        public async Task<DataRepositoryKnownGoods> GetOrUpdateAsync(string ns, DataRepositoryManifest manifest, IReadOnlyList<PodDataRequestInfo> pods, CancellationToken token)
        {
            var lkg = await this.client.GetEndpointAnnotationsAsync(ns, AutoCraneLastKnownGoodEndpointName, token);
            var itemsToAdd = new Dictionary<string, string>();
            foreach (var source in manifest.Sources)
            {
                if (source.Value.Count > 0)
                {
                    bool shouldUpgrade = false;

                    if (!lkg.ContainsKey(source.Key))
                    {
                        // LKG entry does not exist
                        shouldUpgrade = true;
                    }
                    else if (lkg.TryGetValue(source.Key, out var req))
                    {
                        var lkgRequest = DataDownloadRequestDetails.FromBase64Json(req);

                        // if the manifest no longer contains the LKG, we shouldn't send people to download the LKG
                        shouldUpgrade = !source.Value.Any(sv => sv.ArchiveFilePath == lkgRequest?.Path);
                    }

                    if (shouldUpgrade)
                    {
                        var mostRecentData = source.Value.ToList().OrderByDescending(k => k.Timestamp).First();
                        var req = new DataDownloadRequestDetails(mostRecentData.ArchiveFilePath, mostRecentData.Hash);

                        this.logger.LogInformation($"Setting LKG for {source.Key} to hash={req.Hash} filePath={req.Path}");
                        itemsToAdd[source.Key] = req.ToBase64String();
                    }
                }
            }

            foreach (var knownGood in lkg)
            {
                var dataSource = knownGood.Key;
                var currentVersionString = knownGood.Value;

                var requestsForThisSource = pods.Select(p => p.Requests.FirstOrDefault(r => r.Key == dataSource).Value)
                    .Select(r => r == null ? null : DataDownloadRequestDetails.FromBase64Json(r))
                    .ToList();

                var distinctRequestsForThisSource = requestsForThisSource.Select(r => r?.Path).Distinct().ToList();
                var firstRequestForThisSource = requestsForThisSource.FirstOrDefault();
                if (distinctRequestsForThisSource.Count == 1 && firstRequestForThisSource != null)
                {
                    var everyPodHasThisPath = distinctRequestsForThisSource.First();
                    var currentVersion = DataDownloadRequestDetails.FromBase64Json(currentVersionString);
                    if (currentVersion == null)
                    {
                        this.logger.LogError($"Couldn't read current version of data {dataSource}, value: {currentVersionString}");
                    }
                    else
                    {
                        if (currentVersion.Path != everyPodHasThisPath)
                        {
                            this.logger.LogInformation($"Upgrading LKG for {dataSource} to hash={firstRequestForThisSource!.Hash} filePath={firstRequestForThisSource!.Path}");
                            itemsToAdd[dataSource] = firstRequestForThisSource.ToBase64String();
                        }
                    }
                }
            }

            if (itemsToAdd.Any())
            {
                await this.client.PutEndpointAnnotationsAsync(ns, AutoCraneLastKnownGoodEndpointName, itemsToAdd, token);
            }

            var newDict = new Dictionary<string, string>(lkg);
            foreach (var item in itemsToAdd)
            {
                newDict[item.Key] = item.Value;
            }

            return new DataRepositoryKnownGoods(newDict);
        }
    }
}
