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
            foreach (var item in manifest.Sources)
            {
                if (!lkg.ContainsKey(item.Key) && item.Value.Count > 0)
                {
                    var mostRecentData = item.Value.ToList().OrderByDescending(k => k.Timestamp).First();
                    var req = new DataDownloadRequestDetails(mostRecentData.ArchiveFilePath, mostRecentData.Hash);

                    this.logger.LogInformation($"Setting LKG for {item.Key} to hash={req.Hash} filePath={req.Path}");
                    itemsToAdd[item.Key] = req.ToBase64String();
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
                if (distinctRequestsForThisSource.Count == 1)
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
                            var req = requestsForThisSource.First(r => r != null);
                            this.logger.LogInformation($"Upgrading LKG for {dataSource} to hash={req!.Hash} filePath={req!.Path}");
                            itemsToAdd[dataSource] = req.ToBase64String();
                        }
                    }
                }
            }

            if (itemsToAdd.Any())
            {
                await this.client.PutEndpointAnnotationsAsync(ns, AutoCraneLastKnownGoodEndpointName, itemsToAdd, token);
            }

            return new DataRepositoryKnownGoods(lkg.Union(itemsToAdd).ToDictionary(ks => ks.Key, vs => vs.Value));
        }
    }
}
