// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoCrane.Services
{
    internal sealed class DataDownloadRequestFactory : IDataDownloadRequestFactory
    {
        private readonly ILogger<DataDownloadRequestFactory> logger;
        private readonly IOptions<PodIdentifierOptions> thisPodOptions;
        private readonly IPodGetter podGetter;

        public DataDownloadRequestFactory(ILoggerFactory loggerFactory, IOptions<PodIdentifierOptions> thisPodOptions, IPodGetter podGetter)
        {
            this.logger = loggerFactory.CreateLogger<DataDownloadRequestFactory>();
            this.thisPodOptions = thisPodOptions;
            this.podGetter = podGetter;
        }

        public Task<IList<DataDownloadRequest>> GetPodRequestsAsync()
        {
            return this.GetPodRequestsAsync(this.thisPodOptions.Value.Identifier);
        }

        public async Task<IList<DataDownloadRequest>> GetPodRequestsAsync(PodIdentifier pod)
        {
            this.logger.LogInformation($"Getting pod info {pod}");
            var podInfo = await this.podGetter.GetPodAsync(pod);

            this.logger.LogInformation($"Getting {CommonAnnotations.DataStoreLocation}");
            var storeLocation = podInfo.Annotations.First(pi => pi.Key == CommonAnnotations.DataStoreLocation).Value;
            this.logger.LogInformation($"Getting {CommonAnnotations.DataStoreUrl}");
            var storeUrl = podInfo.Annotations.First(pi => pi.Key == CommonAnnotations.DataStoreUrl).Value;

            var dataToGet = podInfo.Annotations.Where(pi => pi.Key.StartsWith(CommonAnnotations.DataDeploymentPrefix)).ToList();

            var list = new List<DataDownloadRequest>();

            foreach (var dataDeployment in dataToGet)
            {
                var name = dataDeployment.Key.Substring(CommonAnnotations.DataDeploymentPrefix.Length);
                var sourceRef = dataDeployment.Value;

                list.Add(new DataDownloadRequest(pod, name, storeUrl, storeLocation, sourceRef));
            }

            return list;
        }
    }
}
