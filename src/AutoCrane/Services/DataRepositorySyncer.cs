// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Services
{
    internal sealed class DataRepositorySyncer : IDataRepositorySyncer
    {
        private readonly ILogger<DataRepositorySyncer> logger;

        public DataRepositorySyncer(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<DataRepositorySyncer>();
        }

        public Task SyncRepoAsync(string name, string repoString, CancellationToken cancellationToken)
        {
            this.logger.LogInformation($"Syncing {name} with repo {repoString}");
            return Task.CompletedTask;
        }
    }
}
