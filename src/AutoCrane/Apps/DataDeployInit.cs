// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Apps
{
    public sealed class DataDeployInit
    {
        private const int ConsecutiveErrorCountBeforeExiting = 5;
        private readonly IDataDownloader dataDownloader;
        private readonly IDataDownloadRequestFactory downloadRequestFactory;
        private readonly IDataLinker dataLinker;
        private readonly IPodAnnotationPutter annotationPutter;
        private readonly ILogger<DataDeployInit> logger;

        public DataDeployInit(ILoggerFactory loggerFactory, IDataDownloader dataDownloader, IDataDownloadRequestFactory downloadRequestFactory, IDataLinker dataLinker, IPodAnnotationPutter annotationPutter)
        {
            this.dataDownloader = dataDownloader;
            this.downloadRequestFactory = downloadRequestFactory;
            this.dataLinker = dataLinker;
            this.annotationPutter = annotationPutter;
            this.logger = loggerFactory.CreateLogger<DataDeployInit>();
        }

        public async Task<int> RunAsync(int iterations = int.MaxValue)
        {
            while (iterations-- > 0)
            {
                var errorCount = 0;

                while (errorCount < ConsecutiveErrorCountBeforeExiting)
                {
                    try
                    {
                        var requests = await this.downloadRequestFactory.GetPodRequestsAsync();
                        if (!requests.Any())
                        {
                            this.logger.LogInformation($"Waiting for requests...");
                            break;
                        }

                        this.logger.LogInformation($"Got {requests.Count} requets...");
                        var sw = Stopwatch.StartNew();
                        foreach (var request in requests)
                        {
                            await this.dataDownloader.DownloadAsync(request);
                            await this.dataLinker.LinkAsync(Path.Combine(request.DataDropFolder, request.Details.Path!.TrimStart(Path.DirectorySeparatorChar)), Path.Combine(request.DataDropFolder, request.Name), CancellationToken.None);
                            var requestB64 = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(request.Details));
                            await this.annotationPutter.PutPodAnnotationAsync($"{CommonAnnotations.DataStatusPrefix}/{request.Name}", requestB64);
                        }

                        sw.Stop();
                        this.logger.LogInformation($"Done in {sw.Elapsed}");
                        return 0;
                    }
                    catch (Exception e)
                    {
                        this.logger.LogError($"Unhandled exception: {e}");
                        errorCount++;
                    }
                }
            }

            this.logger.LogError($"Hit max consecutive error count...exiting...");
            return 1;
        }
    }
}
