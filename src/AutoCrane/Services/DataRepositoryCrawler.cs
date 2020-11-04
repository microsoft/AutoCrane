// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoCrane.Services
{
    internal sealed class DataRepositoryCrawler : BackgroundService, IHostedService
    {
        private readonly ILogger<DataRepositoryCrawler> logger;
        private readonly IOptions<DataRepoOptions> options;
        private readonly IServiceHeartbeat heartbeat;
        private readonly IDataRepositorySyncer repoSyncer;
        private readonly IDataRepositoryManifestWriter manifestWriter;

        public DataRepositoryCrawler(ILoggerFactory loggerFactory, IOptions<DataRepoOptions> options, IServiceHeartbeat heartbeat, IDataRepositorySyncer repoSyncer, IDataRepositoryManifestWriter manifestWriter)
        {
            this.logger = loggerFactory.CreateLogger<DataRepositoryCrawler>();
            this.options = options;
            this.heartbeat = heartbeat;
            this.repoSyncer = repoSyncer;
            this.manifestWriter = manifestWriter;
        }

        public static TimeSpan HeartbeatTimeout { get; } = TimeSpan.FromMinutes(35);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var sourcePath = this.options.Value.SourcePath;
            var archivePath = this.options.Value.ArchivePath;
            if (sourcePath is null)
            {
                this.logger.LogError($"Source path not set.");
                return;
            }

            if (archivePath is null)
            {
                this.logger.LogError($"Archive path not set");
                return;
            }

            Directory.CreateDirectory(sourcePath);
            Directory.CreateDirectory(archivePath);

            // turn "data1:git@https://github.com/microsoft/AutoCrane.git data2:git@https://github.com/dotnet/installer.git"
            // into ["data1"] = git@https://github.com/microsoft/AutoCrane.git
            var sources = this.options.Value.Sources?.Split(';').Select(s => s.Split(':', 2)).Where(ss => ss.Length == 2).Select(ss =>
            {
                return new KeyValuePair<string, string>(ss[0], ss[1]);
            }).ToDictionary(x => x.Key, x => x.Value);

            if (sources is null || !sources.Any())
            {
                this.logger.LogError($"No sources found");
                return;
            }

            this.logger.LogInformation($"DataRepositoryCrawler found {sources.Count} sources: {string.Join(',', sources.Keys)}");

            while (!stoppingToken.IsCancellationRequested)
            {
                var success = true;
                var sourceList = new Dictionary<string, IReadOnlyList<DataRepositorySource>>();
                foreach (var repo in sources)
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        var fullArchivePath = Path.Combine(archivePath, repo.Key);
                        Directory.CreateDirectory(fullArchivePath);
                        var fullSourcePath = Path.Combine(sourcePath, repo.Key);
                        Directory.CreateDirectory(fullSourcePath);
                        var newSources = await this.repoSyncer.SyncRepoAsync(fullSourcePath, fullArchivePath, repo.Value, stoppingToken);
                        sourceList[repo.Key] = newSources;
                    }
                    catch (Exception e)
                    {
                        this.logger.LogError($"Error syncing {repo.Key}: {e}");
                        success = false;
                    }
                }

                try
                {
                    await this.manifestWriter.WriteAsync(new DataRepositoryManifest(sourceList));
                }
                catch (Exception e)
                {
                    this.logger.LogError($"Error writing manifest: {e}");
                    success = false;
                }

                if (success)
                {
                    this.heartbeat.Beat(nameof(DataRepositoryCrawler));
                }

                await Task.Delay(HeartbeatTimeout / 4, stoppingToken);
            }

            this.logger.LogInformation($"Exiting DataRepositoryCrawler");
        }
    }
}
