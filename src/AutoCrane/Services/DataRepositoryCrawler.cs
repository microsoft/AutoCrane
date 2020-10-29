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

        public DataRepositoryCrawler(ILoggerFactory loggerFactory, IOptions<DataRepoOptions> options, IServiceHeartbeat heartbeat, IDataRepositorySyncer repoSyncer)
        {
            this.logger = loggerFactory.CreateLogger<DataRepositoryCrawler>();
            this.options = options;
            this.heartbeat = heartbeat;
            this.repoSyncer = repoSyncer;
        }

        public static TimeSpan HeartbeatTimeout { get; } = TimeSpan.FromMinutes(10);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this.logger.LogInformation("DataRepositoryCrawler ExecuteAsync start");

            var repoDir = this.options.Value.Path;
            if (repoDir is null || !Directory.Exists(repoDir))
            {
                this.logger.LogError($"Repo path not set or does not exist: {repoDir}");
            }

            // turn "data1:git@https://github.com/microsoft/AutoCrane.git data2:git@https://github.com/dotnet/installer.git"
            // into ["data1"] = git@https://github.com/microsoft/AutoCrane.git
            var sources = this.options.Value.Sources?.Split(' ').Select(s => s.Split(':', 2)).Where(ss => ss.Length == 2).Select(ss =>
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
                foreach (var repo in sources)
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        await this.repoSyncer.SyncRepoAsync(repo.Key, repo.Value, stoppingToken);
                    }
                    catch (Exception e)
                    {
                        this.logger.LogError($"Error syncing {repo.Key}: {e}");
                        success = false;
                    }
                }

                if (success)
                {
                    this.heartbeat.Beat(nameof(DataRepositoryCrawler));
                }

                await Task.Delay(10_000, stoppingToken);
            }

            this.logger.LogInformation("DataRepositoryCrawler ExecuteAsync stop");
        }
    }
}
