// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Services
{
    internal sealed class DataRepositoryCrawler : BackgroundService, IHostedService
    {
        private readonly ILogger<DataRepositoryCrawler> logger;

        public DataRepositoryCrawler(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<DataRepositoryCrawler>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this.logger.LogInformation("DataRepositoryCrawler ExecuteAsync");
            while (true)
            {
                this.logger.LogInformation("DataRepositoryCrawler ExecuteAsync fake work");
                await Task.Delay(10_000, stoppingToken);
            }
        }
    }
}
