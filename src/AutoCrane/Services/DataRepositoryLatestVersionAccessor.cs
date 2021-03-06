﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Services
{
    internal sealed class DataRepositoryLatestVersionAccessor : IDataRepositoryLatestVersionAccessor
    {
        private const string AutoCraneDataDeployEndpointName = "autocranelatest";
        private readonly ILogger<DataRepositoryLatestVersionAccessor> logger;
        private readonly KubernetesClient client;

        public DataRepositoryLatestVersionAccessor(ILoggerFactory loggerFactory, KubernetesClient client)
        {
            this.logger = loggerFactory.CreateLogger<DataRepositoryLatestVersionAccessor>();
            this.client = client;
        }

        public async Task<DataRepositoryLatestVersionInfo> GetOrUpdateAsync(string ns, DataRepositoryManifest manifest, CancellationToken token)
        {
            var currentVer = await this.client.GetEndpointAnnotationsAsync(ns, AutoCraneDataDeployEndpointName, token);
            var itemsToAdd = new Dictionary<string, string>();
            foreach (var item in manifest.Sources)
            {
                var recentData = item.Value.ToList().OrderByDescending(k => k.Timestamp);
                if (!recentData.Any())
                {
                    this.logger.LogWarning($"Missing recent data for {item.Key}/{item.Value}");
                    continue;
                }

                var mostRecentData = recentData.First();
                var req = new DataDownloadRequestDetails(mostRecentData.ArchiveFilePath, mostRecentData.Hash);

                var latestVersion = req.ToBase64String();
                if (currentVer.TryGetValue(item.Key, out var currentVerString))
                {
                    var currentVerJson = DataDownloadRequestDetails.FromBase64Json(currentVerString);
                    if (currentVerJson?.Path == req.Path)
                    {
                        continue;
                    }
                }

                this.logger.LogInformation($"Setting Latest for {item.Key} to hash={req.Hash} filePath={req.Path}");
                itemsToAdd[item.Key] = latestVersion;
            }

            if (itemsToAdd.Any())
            {
                await this.client.PutEndpointAnnotationsAsync(ns, AutoCraneDataDeployEndpointName, itemsToAdd, token);
            }

            var union = itemsToAdd;
            foreach (var item in currentVer)
            {
                union.TryAdd(item.Key, item.Value);
            }

            return new DataRepositoryLatestVersionInfo(union);
        }
    }
}
