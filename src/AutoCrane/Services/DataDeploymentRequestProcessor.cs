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
        private const int MaxRequestLoopCount = 3;
        private readonly ILogger<DataDeploymentRequestProcessor> logger;
        private readonly IDataDownloader dataDownloader;
        private readonly IDataDownloadRequestFactory downloadRequestFactory;
        private readonly IDataLinker dataLinker;

        public DataDeploymentRequestProcessor(ILoggerFactory loggerFactory, IDataDownloader dataDownloader, IDataDownloadRequestFactory downloadRequestFactory, IDataLinker dataLinker)
        {
            this.logger = loggerFactory.CreateLogger<DataDeploymentRequestProcessor>();
            this.dataDownloader = dataDownloader;
            this.downloadRequestFactory = downloadRequestFactory;
            this.dataLinker = dataLinker;
        }

        public async Task HandleRequestsAsync(CancellationToken token)
        {
            IList<DataDownloadRequest>? requests = null;
            var loopCount = 0;
            while (requests is null || requests.Any(r => r.Details is null))
            {
                this.logger.LogInformation($"Getting data requests...");
                requests = await this.downloadRequestFactory.GetPodRequestsAsync();

                this.logger.LogInformation($"Got {requests.Count} data requests... {requests.Where(r => r.Details is null).Count()} where details == null.");
                await Task.Delay(TimeSpan.FromSeconds(GetRequestLoopSeconds), token);
                token.ThrowIfCancellationRequested();
                loopCount++;

                // if AutoCrane hasn't given us any request info after this long, throw an exception
                if (loopCount > MaxRequestLoopCount)
                {
                    throw new TimeoutException($"Timeout waiting for DownloadDataRequests with details. Is the AutoCrane orchestrator running and serving data requests?");
                }
            }

            this.logger.LogInformation($"Got {requests.Count} requets...");
            var sw = Stopwatch.StartNew();
            foreach (var request in requests)
            {
                await this.dataDownloader.DownloadAsync(request, token);
                await this.dataLinker.LinkAsync(request.ExtractionLocation, Path.Combine(request.DataDropFolder, request.LocalName), token);

                // is writing an annotation to indicate success too much access for a sidecar?
                // var requestB64 = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(request.Details));
                // await this.annotationPutter.PutPodAnnotationAsync($"{CommonAnnotations.DataStatusPrefix}/{request.Name}", requestB64);
            }

            sw.Stop();
            this.logger.LogInformation($"Done in {sw.Elapsed}");
        }
    }
}
