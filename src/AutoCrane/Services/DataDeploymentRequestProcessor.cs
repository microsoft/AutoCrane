// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Services
{
    internal sealed class DataDeploymentRequestProcessor : IDataDeploymentRequestProcessor
    {
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
            var requests = await this.downloadRequestFactory.GetPodRequestsAsync();
            if (!requests.Any())
            {
                this.logger.LogInformation($"Waiting for requests...");
                return;
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
