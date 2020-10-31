﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
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
        private readonly IFileHasher fileHasher;

        public DataDownloader(ILoggerFactory loggerFactory, IProcessRunner runner, IFileHasher fileHasher)
        {
            this.client = new HttpClient();
            this.logger = loggerFactory.CreateLogger<DataDownloader>();
            this.runner = runner;
            this.fileHasher = fileHasher;
        }

        public async Task DownloadAsync(DataDownloadRequest request)
        {
            if (request.Details.Hash is null || request.Details.Path is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var dropArchive = Path.Combine(request.DataDropFolder, request.Details.Hash);
            this.logger.LogInformation($"Checking if already downloaded to {dropArchive}");
            if (!File.Exists(dropArchive))
            {
                this.logger.LogInformation($"Already downloaded to {dropArchive}");
            }
            else
            {
                try
                {
                    var dropUrl = $"http://datarepo/{request.Details.Path}";
                    this.logger.LogInformation($"Downloading {dropUrl} to {dropArchive}");
                    var data = await this.client.GetAsync(dropUrl);
                    data.EnsureSuccessStatusCode();
                    using var fs = File.Create(dropArchive);
                    await data.Content.CopyToAsync(fs);
                    fs.Close();
                    await this.VerifyHashAsync(dropArchive, request.Details.Hash);
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

        private async Task VerifyHashAsync(string dropLocation, string hashToMatch)
        {
            var hash = await this.fileHasher.GetAsync(dropLocation);
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
