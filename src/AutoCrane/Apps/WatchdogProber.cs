// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Apps
{
    public sealed class WatchdogProber
    {
        private const int ConsecutiveErrorCountBeforeExiting = 5;
        private const int IterationLoopSeconds = 10;
        private const int WatchdogFailuresBeforeEviction = 3;
        private readonly IAutoCraneConfig config;
        private readonly IPodGetter podGetter;
        private readonly ILogger<Orchestrator> logger;

        public WatchdogProber(IAutoCraneConfig config, ILoggerFactory loggerFactory, IPodGetter podGetter)
        {
            this.config = config;
            this.podGetter = podGetter;
            this.logger = loggerFactory.CreateLogger<Orchestrator>();
        }

        public async Task<int> RunAsync(int iterations = int.MaxValue)
        {
            var errorCount = 0;
            if (!this.config.Namespaces.Any())
            {
                this.logger.LogError($"No namespaces configured to watch... set env var AutoCrane__Namespaces to a comma-separated value");
                return 3;
            }

            while (iterations > 0)
            {
                if (errorCount > ConsecutiveErrorCountBeforeExiting)
                {
                    this.logger.LogError($"Hit max consecutive error count...exiting...");
                    return 2;
                }

                try
                {
                    foreach (var ns in this.config.Namespaces)
                    {
                        var pods = await this.podGetter.GetPodsAsync(ns);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(IterationLoopSeconds));
                    iterations--;
                    errorCount = 0;
                }
                catch (Exception e)
                {
                    this.logger.LogError($"Unhandled exception: {e}");
                    errorCount++;
                    await Task.Delay(TimeSpan.FromSeconds(IterationLoopSeconds));
                }
            }

            return 0;
        }
    }
}
