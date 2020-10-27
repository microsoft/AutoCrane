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
        private readonly IPodGetter podGetter;
        private readonly IDataDownloader dataDownloader;
        private readonly ILogger<DataDeployInit> logger;
        private readonly PodIdentifierOptions podIdentifier;

        public DataDeployInit(IAutoCraneConfig config, ILoggerFactory loggerFactory, IPodGetter podGetter, IOptions<PodIdentifierOptions> podOptions, IDataDownloader dataDownloader)
        {
            this.config = config;
            this.podGetter = podGetter;
            this.dataDownloader = dataDownloader;
            this.logger = loggerFactory.CreateLogger<DataDeployInit>();
            this.podIdentifier = podOptions.Value;
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

                    this.logger.LogInformation($"Getting pod info {this.podIdentifier.Identifier}");
                    var podInfo = await this.podGetter.GetPodAsync(this.podIdentifier.Identifier);

                    this.logger.LogInformation($"Getting {CommonAnnotations.DataStoreLocation}");
                    var storeLocation = podInfo.Annotations.First(pi => pi.Key == CommonAnnotations.DataStoreLocation).Value;
                    this.logger.LogInformation($"Getting {CommonAnnotations.DataStoreUrl}");
                    var storeUrl = podInfo.Annotations.First(pi => pi.Key == CommonAnnotations.DataStoreUrl).Value;

                    var dataToGet = podInfo.Annotations.Where(pi => pi.Key.StartsWith(CommonAnnotations.DataDeploymentPrefix)).ToList();

                    foreach (var dataDeployment in dataToGet)
                    {
                        var name = dataDeployment.Key.Substring(CommonAnnotations.DataDeploymentPrefix.Length);
                        var splits = dataDeployment.Value.Split(':', 2);
                        if (splits.Length != 2)
                        {
                            this.logger.LogError($"Invalid data deployment spec: {dataDeployment.Key}: {dataDeployment.Value}");
                        }

                        var method = splits[0];
                        var sourceRef = splits[1];

                        await this.dataDownloader.DownloadAsync(name, method, sourceRef, storeUrl, storeLocation);
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
