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
using Microsoft.Extensions.Logging;

namespace AutoCrane.Services
{
    internal sealed class DataRepositorySyncer : IDataRepositorySyncer
    {
        private readonly ILogger<DataRepositorySyncer> logger;
        private readonly IEnumerable<IDataRepositoryFetcher> fetchers;

        public DataRepositorySyncer(ILoggerFactory loggerFactory, IEnumerable<IDataRepositoryFetcher> fetchers)
        {
            this.logger = loggerFactory.CreateLogger<DataRepositorySyncer>();
            this.fetchers = fetchers;
        }

        public async Task<IReadOnlyList<DataRepositorySource>> SyncRepoAsync(string scratchDir, string archiveDir, string repoString, CancellationToken cancellationToken)
        {
            this.logger.LogInformation($"Syncing {scratchDir} with repo {repoString}");
            var repoSplits = repoString.Split('@', 2);
            if (repoSplits.Length != 2)
            {
                throw new InvalidOperationException($"Invalid repo ref specifier: '{repoString}'. Format 'method:url'");
            }

            var list = new List<DataRepositorySource>();

            var protocol = repoSplits[0];
            var fetcher = this.fetchers.FirstOrDefault(f => f.CanFetch(protocol));
            if (fetcher is null)
            {
                throw new InvalidOperationException($"No fetcher for protocol {protocol}");
            }

            var newEntries = await fetcher.FetchAsync(repoSplits[1], scratchDir, archiveDir, cancellationToken);
            list.AddRange(newEntries);

            return list;
        }
    }
}
