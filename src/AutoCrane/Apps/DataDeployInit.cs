// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Apps
{
    public sealed class DataDeployInit
    {
        private const int ConsecutiveErrorCountBeforeExiting = 3;
        private const int IterationLoopSeconds = 15;
        private readonly ILogger<DataDeployInit> logger;
        private readonly IDataDeploymentRequestProcessor dataDeploymentRequestProcessor;

        public DataDeployInit(ILoggerFactory loggerFactory, IDataDeploymentRequestProcessor dataDeploymentRequestProcessor)
        {
            this.logger = loggerFactory.CreateLogger<DataDeployInit>();
            this.dataDeploymentRequestProcessor = dataDeploymentRequestProcessor;
        }

        public async Task<int> RunAsync(int iterations = int.MaxValue)
        {
            var errorCount = 0;

            while (errorCount < ConsecutiveErrorCountBeforeExiting)
            {
                try
                {
                    await this.dataDeploymentRequestProcessor.HandleRequestsAsync(CancellationToken.None);
                    return 0;
                }
                catch (Exception e)
                {
                    this.logger.LogError($"Unhandled exception: {e}");
                    errorCount++;
                }

                await Task.Delay(TimeSpan.FromSeconds(IterationLoopSeconds));
            }

            this.logger.LogError($"Hit max consecutive error count...exiting...");
            return 1;
        }
    }
}
