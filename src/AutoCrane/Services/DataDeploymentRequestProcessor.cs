// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Services
{
    internal sealed class DataDeploymentRequestProcessor : IDataDeploymentRequestProcessor
    {
        private const int GetRequestLoopSeconds = 15;
        private const int MaxRequestLoopCount = 5;
        private readonly ILogger<DataDeploymentRequestProcessor> logger;
        private readonly IDataDownloader dataDownloader;
        private readonly IDataDownloadRequestFactory downloadRequestFactory;
        private readonly IDataLinker dataLinker;
        private readonly IDataRepositoryManifestFetcher manifestFetcher;

        public DataDeploymentRequestProcessor(ILoggerFactory loggerFactory, IDataDownloader dataDownloader, IDataDownloadRequestFactory downloadRequestFactory, IDataLinker dataLinker, IDataRepositoryManifestFetcher manifestFetcher)
        {
            this.logger = loggerFactory.CreateLogger<DataDeploymentRequestProcessor>();
            this.dataDownloader = dataDownloader;
            this.downloadRequestFactory = downloadRequestFactory;
            this.dataLinker = dataLinker;
            this.manifestFetcher = manifestFetcher;
        }

        public async Task HandleRequestsAsync(CancellationToken token)
        {
            IList<DataDownloadRequest>? requests = null;
            var loopCount = 0;

            while (requests is null || requests.Any(r => r.Details is null))
            {
                this.logger.LogInformation($"Getting data requests...");
                requests = await this.downloadRequestFactory.GetPodRequestsAsync();

                this.logger.LogInformation($"Requests served: {requests.Where(r => r.Details is not null).Count()}/{requests.Count}...");
                await Task.Delay(TimeSpan.FromSeconds(GetRequestLoopSeconds), token);
                token.ThrowIfCancellationRequested();
                loopCount++;

                // if AutoCrane hasn't given us any request info after this long, throw an exception
                if (loopCount > MaxRequestLoopCount)
                {
                    throw new TimeoutException($"Timeout waiting for DownloadDataRequests with details. Is the AutoCrane orchestrator running and serving data requests?");
                }
            }

            this.logger.LogInformation($"Handling {requests.Count} data deployment requets...");
            var sw = Stopwatch.StartNew();

            // try to cleanup first
            if (requests.Any())
            {
                var dropFolder = requests.First().DataDropFolder;
                if (Directory.Exists(dropFolder))
                {
                    this.logger.LogInformation($"First cleaning up {dropFolder}...");

                    var manifest = await this.manifestFetcher.FetchAsync(token);
                    var manifestFileList = manifest.Sources.Values.SelectMany(drs => drs);
                    if (manifestFileList is null)
                    {
                        throw new ArgumentNullException(nameof(manifestFileList));
                    }

                    var shouldNotDeleteDirectories = manifestFileList.Select(src => Path.Combine(dropFolder, src.ArchiveFilePath)).ToHashSet();
                    var shouldNotDeleteFiles = manifestFileList.Select(src => this.dataDownloader.GetDropDownloadArchiveName(dropFolder, src.Hash)).ToHashSet();

                    this.logger.LogInformation($"directories that may exist {string.Join(';', shouldNotDeleteDirectories)}");
                    this.logger.LogInformation($"files that may exist {string.Join(';', shouldNotDeleteFiles)}");

                    var filesToDelete = Directory.GetFiles(dropFolder).Where(file => !shouldNotDeleteFiles.Contains(file)).ToList();
                    var dirsToDelete = Directory.GetDirectories(dropFolder).Where(file => !shouldNotDeleteDirectories.Contains(file)).ToList();
                    foreach (var file in filesToDelete)
                    {
                        if (File.Exists(file))
                        {
                            this.logger.LogInformation($"Deleting file: {file}");
                            File.Delete(file);
                        }
                        else
                        {
                            this.logger.LogError($"Could not find: {file}");
                        }
                    }

                    foreach (var dir in dirsToDelete)
                    {
                        if (Directory.Exists(dir))
                        {
                            this.logger.LogInformation($"Deleting directory: {dir}");
                            Directory.Delete(dir, recursive: true);
                        }
                        else
                        {
                            this.logger.LogError($"Could not find: {dir}");
                        }
                    }
                }
            }

            foreach (var request in requests)
            {
                await this.dataDownloader.DownloadAsync(request, token);
                await this.dataLinker.LinkAsync(request.ExtractionLocation, Path.Combine(request.DataDropFolder, request.DataSource), token);
            }

            sw.Stop();
            this.logger.LogInformation($"Done in {sw.Elapsed}");
        }
    }
}
