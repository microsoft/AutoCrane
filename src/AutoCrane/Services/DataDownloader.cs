// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
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

        public DataDownloader(ILoggerFactory loggerFactory)
        {
            this.client = new HttpClient();
            this.logger = loggerFactory.CreateLogger<DataDownloader>();
        }

        public async Task DownloadAsync(DataDownloadRequest request)
        {
#if false
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

#endif
            var dropLocation = Path.Combine(request.StoreLocation, request.SourceRef);
            var dropArchive = dropLocation + ".tar.xz";
            this.logger.LogInformation($"Checking if already downloaded to {dropLocation}");
            if (Directory.Exists(dropLocation) && !File.Exists(dropArchive))
            {
                this.logger.LogInformation($"Already downloaded to {dropLocation}");
            }
            else
            {
                try
                {
                    var dropUrl = $"{request.StoreUrl}/{request.SourceRef}";
                    this.logger.LogInformation($"Downloading {dropUrl} to {dropLocation}");
                    var data = await this.client.GetAsync(dropUrl);
                    data.EnsureSuccessStatusCode();
                    using var fs = new FileStream(dropArchive, FileMode.CreateNew);
                    await data.Content.CopyToAsync(fs);
                    fs.Close();
                    await VerifyHashAsync(dropArchive, request.HashToMatch);
                    await this.ExtractArchiveAsync(dropArchive, dropLocation);
                    File.Delete(dropArchive);
                }
                catch (Exception e)
                {
                    this.logger.LogError($"Exception downloading {request.Name}: {e}");

                    // clean up partial downloads
                    if (File.Exists(dropArchive))
                    {
                        File.Delete(dropArchive);
                    }

                    if (Directory.Exists(dropLocation))
                    {
                        Directory.Delete(dropLocation, recursive: true);
                    }

                    throw;
                }
            }
        }

        private static async Task VerifyHashAsync(string dropLocation, string hashToMatch)
        {
            using var fs = new FileStream(dropLocation, FileMode.Open);
            using var sha = SHA256.Create();
            var hashBinary = await sha.ComputeHashAsync(fs);
            var hash = Convert.ToHexString(hashBinary);
            if (hashToMatch != hash)
            {
                throw new Exception($"Hash mismatch on {dropLocation}, expected {hashToMatch}, actual {hash}");
            }
        }

        private Task ExtractArchiveAsync(string dropArchive, string dropLocation)
        {
            using (var process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "/bin/tar",
                },
            })
            {
                process.StartInfo.ArgumentList.Add("-x");
                process.StartInfo.ArgumentList.Add("-C");
                process.StartInfo.ArgumentList.Add(dropLocation);
                process.StartInfo.ArgumentList.Add("-f");
                process.StartInfo.ArgumentList.Add(dropArchive);
                process.Start();
                this.logger.LogInformation($"Running: tar -x -C {dropLocation} -f {dropArchive}");
                return process.WaitForExitAsync();
            }
        }
    }
}
