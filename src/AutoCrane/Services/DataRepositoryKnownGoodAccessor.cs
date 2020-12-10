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
using Microsoft.Extensions.Logging;

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

        public async Task<DataRepositoryKnownGoods> GetOrCreateAsync(string ns, DataRepositoryManifest manifest, CancellationToken token)
        {
            var lkg = await this.client.GetEndpointAnnotationsAsync(ns, AutoCraneLastKnownGoodEndpointName, token);
            var itemsToAdd = new Dictionary<string, string>();
            foreach (var item in manifest.Sources)
            {
                if (!lkg.ContainsKey(item.Key) && item.Value.Count > 0)
                {
                    var mostRecentData = item.Value.ToList().OrderByDescending(k => k.Timestamp).First();
                    var req = new DataDownloadRequestDetails()
                    {
                        Hash = mostRecentData.Hash,
                        Path = mostRecentData.ArchiveFilePath,
                    };

                    this.logger.LogInformation($"Setting LKG for {item.Key} to hash={req.Hash} filePath={req.Path}");
                    itemsToAdd[item.Key] = req.ToBase64String();
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
