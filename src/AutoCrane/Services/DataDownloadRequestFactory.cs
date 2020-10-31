// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Converters;

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
            var dropFolder = podInfo.Annotations.First(pi => pi.Key == CommonAnnotations.DataStoreLocation).Value;

            var dataToGet = podInfo.Annotations.Where(pi => pi.Key.StartsWith(CommonAnnotations.DataRequestPrefix)).ToList();

            var list = new List<DataDownloadRequest>();

            foreach (var dataDeployment in dataToGet)
            {
                var localDeploymentName = dataDeployment.Key.Replace(CommonAnnotations.DataRequestPrefix, string.Empty);
                var dataDeploymentAnnotation = $"{CommonAnnotations.DataDeploymentPrefix}{localDeploymentName}";
                var repoName = podInfo.Annotations.Where(pa => pa.Key == dataDeploymentAnnotation).FirstOrDefault().Value;
                var utf8json = Convert.FromBase64String(dataDeployment.Value);
                var details = JsonSerializer.Deserialize<DataDownloadRequestDetails>(utf8json);
                if (details is null || details.Hash is null || details.Path is null || repoName is null)
                {
                    continue;
                }

                var extractionLocation = Path.Combine(dropFolder, details.Path.Replace(Path.PathSeparator, '_'));
                list.Add(new DataDownloadRequest(pod, localDeploymentName, repoName, dropFolder, extractionLocation, details));
            }

            return list;
        }
    }
}
