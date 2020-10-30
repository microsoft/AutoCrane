﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoCrane.Services
{
    internal sealed class DataRepositoryManifestFetcher : IDataRepositoryManifestFetcher
    {
        private readonly ILogger<DataRepositoryManifestFetcher> logger;
        private readonly HttpClient client;
        private readonly IDataRepositoryManifestReaderFactory manifestReaderFactory;

        public DataRepositoryManifestFetcher(ILoggerFactory loggerFactory, IOptions<DataRepoOptions> options, IDataRepositoryManifestReaderFactory manifestReaderFactory)
        {
            this.logger = loggerFactory.CreateLogger<DataRepositoryManifestFetcher>();
            var rootDirectory = options.Value.ArchivePath;
            if (rootDirectory is null)
            {
                throw new ArgumentNullException($"DataRepoOptions:ArchivePath is null");
            }

            this.client = new HttpClient();
            this.manifestReaderFactory = manifestReaderFactory;
        }

        public async Task<DataRepositoryManifest> FetchAsync(CancellationToken token)
        {
            var manifestUrl = $"http://datarepo/.manifest";
            this.logger.LogInformation($"Downloading {manifestUrl}");
            var resp = await this.client.GetAsync(manifestUrl, token);
            resp.EnsureSuccessStatusCode();
            using var dropReader = this.manifestReaderFactory.FromStream(resp.Content.ReadAsStream(token));
            var manifest = await dropReader.ReadAsync(token);
            return manifest;
        }
    }
}
