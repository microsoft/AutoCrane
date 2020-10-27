// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoCrane.Apps
{
    public sealed class DataDeployInit
    {
        private const int ConsecutiveErrorCountBeforeExiting = 5;
        private readonly HttpClient httpClient;
        private readonly IAutoCraneConfig config;
        private readonly IDataDownloader dataDownloader;
        private readonly IDataDownloadRequestFactory downloadRequestFactory;
        private readonly ILogger<DataDeployInit> logger;

        public DataDeployInit(IAutoCraneConfig config, ILoggerFactory loggerFactory, IDataDownloader dataDownloader, IDataDownloadRequestFactory downloadRequestFactory)
        {
            this.config = config;
            this.dataDownloader = dataDownloader;
            this.downloadRequestFactory = downloadRequestFactory;
            this.logger = loggerFactory.CreateLogger<DataDeployInit>();
            this.httpClient = new HttpClient();
        }

        public async Task<int> RunAsync(int iterations = int.MaxValue)
        {
            var errorCount = 0;
            while (errorCount < ConsecutiveErrorCountBeforeExiting)
            {
                try
                {
                    var sw = Stopwatch.StartNew();

                    var requests = await this.downloadRequestFactory.GetPodRequestsAsync();
                    foreach (var request in requests)
                    {
                        await this.dataDownloader.DownloadAsync(request);
                    }

                    sw.Stop();
                    this.logger.LogInformation($"Done in {sw.Elapsed}");
                    iterations--;
                    errorCount = 0;
                }
                catch (Exception e)
                {
                    this.logger.LogError($"Unhandled exception: {e}");
                    errorCount++;
                }
            }

            this.logger.LogError($"Hit max consecutive error count...exiting...");
            return 1;
        }
    }
}
