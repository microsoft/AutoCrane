// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AutoCrane.Exceptions;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Services
{
    internal sealed class DataDownloader : IDataDownloader
    {
        private readonly HttpClient client;
        private readonly ILogger<DataDownloader> logger;
        private readonly IDropManifestReaderFactory dropManifestReaderFactory;

        public DataDownloader(ILoggerFactory loggerFactory, IDropManifestReaderFactory dropManifestReaderFactory)
        {
            this.client = new HttpClient();
            this.logger = loggerFactory.CreateLogger<DataDownloader>();
            this.dropManifestReaderFactory = dropManifestReaderFactory;
        }

        public async Task<DataDownloadResult> DownloadAsync(DataDownloadRequest request)
        {
            var manifestUrl = $"{request.StoreUrl}/.manifest";
            this.logger.LogInformation($"Downloading {manifestUrl}");
            var manifest = await this.client.GetAsync(manifestUrl);
            manifest.EnsureSuccessStatusCode();
            using var dropReader = this.dropManifestReaderFactory.FromStream(manifest.Content.ReadAsStream());
            var mapping = dropReader.Read().FirstOrDefault(drop => drop.RefName == request.SourceRef);
            if (mapping == null)
            {
                throw new DataMappingNotFoundException($"{request.Pod} could not find data ref {request.SourceRef} for data deployment {request.Name}");
            }

            var dropLocation = Path.Combine(request.StoreLocation, mapping.MapTo);
            this.logger.LogInformation($"Checking if already downloaded to {dropLocation}");
            if (File.Exists(mapping.MapTo))
            {
                this.logger.LogInformation($"Already downloaded to {dropLocation}");
            }
            else
            {
                try
                {
                    var dropUrl = $"{request.StoreUrl}/{mapping.MapTo}";
                    this.logger.LogInformation($"Downloading {dropUrl} to {dropLocation}");
                    var data = await this.client.GetAsync(dropUrl);
                    data.EnsureSuccessStatusCode();
                    using var fs = new FileStream(dropLocation, FileMode.CreateNew);
                    await data.Content.CopyToAsync(fs);
                }
                catch (Exception)
                {
                    // clean up partial downloads
                    if (File.Exists(dropLocation))
                    {
                        File.Delete(dropLocation);
                    }

                    throw;
                }
            }

            return new DataDownloadResult(request.Pod, request.Name, mapping.MapTo);
        }
    }
}
