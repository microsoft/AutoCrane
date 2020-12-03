// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Services
{
    internal sealed class DataDeploymentBackgroundSync : BackgroundService, IHostedService
    {
        private readonly ILogger<DataDeploymentBackgroundSync> logger;
        private readonly IServiceHeartbeat heartbeat;
        private readonly IDataDeploymentRequestProcessor deploymentRequestProcessor;

        public DataDeploymentBackgroundSync(ILoggerFactory loggerFactory, IServiceHeartbeat heartbeat, IDataDeploymentRequestProcessor deploymentRequestProcessor)
        {
            this.logger = loggerFactory.CreateLogger<DataDeploymentBackgroundSync>();
            this.heartbeat = heartbeat;
            this.deploymentRequestProcessor = deploymentRequestProcessor;
        }

        public static TimeSpan HeartbeatTimeout { get; } = TimeSpan.FromMinutes(35);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this.logger.LogInformation($"Running deployment sync");

            while (!stoppingToken.IsCancellationRequested)
            {
                var success = true;
                try
                {
                    await this.deploymentRequestProcessor.HandleRequestsAsync(stoppingToken);
                }
                catch (Exception e)
                {
                    this.logger.LogError($"Error processing requests: {e}");
                    success = false;
                }

                if (success)
                {
                    this.heartbeat.Beat(nameof(DataDeploymentBackgroundSync));
                }

                var sleepTime = HeartbeatTimeout / 10;
                this.logger.LogInformation($"Sleeping for {sleepTime}");
                await Task.Delay(sleepTime, stoppingToken);
            }

            this.logger.LogInformation($"Exiting deployment sync");
        }
    }
}
