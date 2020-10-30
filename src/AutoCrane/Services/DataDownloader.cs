// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Services
{
    internal sealed class DataDownloader : IDataDownloader
    {
        private readonly HttpClient client;
        private readonly ILogger<DataDownloader> logger;
        private readonly IProcessRunner runner;

        public DataDownloader(ILoggerFactory loggerFactory, IProcessRunner runner)
        {
            this.client = new HttpClient();
            this.logger = loggerFactory.CreateLogger<DataDownloader>();
            this.runner = runner;
        }

        public async Task DownloadAsync(DataDownloadRequest request)
        {
            var dropArchive = Path.Combine(request.DataDropFolder, request.HashToMatch);
            this.logger.LogInformation($"Checking if already downloaded to {dropArchive}");
            if (!File.Exists(dropArchive))
            {
                this.logger.LogInformation($"Already downloaded to {dropArchive}");
            }
            else
            {
                try
                {
                    var dropUrl = $"http://{request.DataRepositoryHostname}/{request.DataRepositoryFilename}";
                    this.logger.LogInformation($"Downloading {dropUrl} to {dropArchive}");
                    var data = await this.client.GetAsync(dropUrl);
                    data.EnsureSuccessStatusCode();
                    using var fs = File.Create(dropArchive);
                    await data.Content.CopyToAsync(fs);
                    fs.Close();
                    await VerifyHashAsync(dropArchive, request.HashToMatch);
                    this.logger.LogInformation($"Downloading {dropArchive} to {request.ExtractionLocation}");
                    await this.ExtractArchiveAsync(dropArchive, request.ExtractionLocation, CancellationToken.None);
                }
                catch (Exception e)
                {
                    this.logger.LogError($"Exception downloading {request.Name}: {e}");

                    // clean up partial downloads
                    if (File.Exists(dropArchive))
                    {
                        File.Delete(dropArchive);
                    }

                    if (Directory.Exists(request.ExtractionLocation))
                    {
                        Directory.Delete(request.ExtractionLocation, recursive: true);
                    }

                    throw;
                }
            }
        }

        private static async Task VerifyHashAsync(string dropLocation, string hashToMatch)
        {
            using var fs = File.OpenRead(dropLocation);
            using var sha = SHA256.Create();
            var hashBinary = await sha.ComputeHashAsync(fs);
            var hash = Convert.ToHexString(hashBinary);
            if (hashToMatch != hash)
            {
                throw new Exception($"Hash mismatch on {dropLocation}, expected {hashToMatch}, actual {hash}");
            }
        }

        private async Task ExtractArchiveAsync(string dropArchive, string dropLocation, CancellationToken token)
        {
            var result = await this.runner.RunAsync("/bin/tar", null, new string[] { "-x", "-C", dropLocation, "-f", dropArchive }, token);
            result.ThrowIfFailed();
        }
    }
}
