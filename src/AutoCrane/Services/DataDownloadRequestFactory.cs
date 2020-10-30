// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                var name = dataDeployment.Key.Replace(CommonAnnotations.DataRequestPrefix, string.Empty);
                var splits = dataDeployment.Value.Split('/', 3);
                if (splits.Length == 3)
                {
                    var repoFilename = splits[1];
                    var extractionLocation = repoFilename.Replace(Path.PathSeparator, '_');
                    list.Add(new DataDownloadRequest(pod, name, repoHostname: splits[2], dropFolder: dropFolder, repoFilename: repoFilename, hashToMatch: splits[0], extractionLocation: extractionLocation));
                }
            }

            return list;
        }
    }
}
