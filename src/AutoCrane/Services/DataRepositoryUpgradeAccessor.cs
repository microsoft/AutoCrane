﻿// Copyright (c) Microsoft Corporation.
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
    internal sealed class DataRepositoryUpgradeAccessor : IDataRepositoryUpgradeAccessor
    {
        private readonly ILogger<DataRepositoryUpgradeAccessor> logger;
        private readonly KubernetesClient client;

        public DataRepositoryUpgradeAccessor(ILoggerFactory loggerFactory, KubernetesClient client)
        {
            this.logger = loggerFactory.CreateLogger<DataRepositoryUpgradeAccessor>();
            this.client = client;
        }

        public async Task<DataRepositoryUpgradeInfo> GetOrUpdateAsync(string ns, DataRepositoryManifest manifest, CancellationToken token)
        {
            var lkg = await this.client.GetLastUpgradeAsync(ns, token);
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
                    itemsToAdd[item.Key] = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(req));
                }
            }

            if (itemsToAdd.Any())
            {
                await this.client.PutLastUpgradeAsync(ns, itemsToAdd, token);
            }

            return new DataRepositoryUpgradeInfo(lkg.Union(itemsToAdd).ToDictionary(ks => ks.Key, vs => vs.Value));
        }
    }
}
