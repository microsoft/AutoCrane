// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Apps
{
    public sealed class Orchestrator
    {
        private const int ConsecutiveErrorCountBeforeExiting = 5;
        private const int IterationLoopSeconds = 10;
        private const int WatchdogFailuresBeforeEviction = 3;
        private readonly IAutoCraneConfig config;
        private readonly IKubernetesClient client;
        private readonly ILogger<Orchestrator> logger;

        public Orchestrator(IAutoCraneConfig config, ILoggerFactory loggerFactory, IKubernetesClient client)
        {
            this.config = config;
            this.client = client;
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

            var podsWithFailingWatchdog = new Queue<List<PodIdentifier>>();

            while (iterations > 0)
            {
                if (errorCount > ConsecutiveErrorCountBeforeExiting)
                {
                    this.logger.LogError($"Hit max consecutive error count...exiting...");
                    return 2;
                }

                try
                {
                    var thisIterationFailingPods = new List<PodIdentifier>();
                    foreach (var ns in this.config.Namespaces)
                    {
                        var failingPods = await this.client.GetFailingPodsAsync(ns);
                        thisIterationFailingPods.AddRange(failingPods.Select(fp => new PodIdentifier(ns, fp)));
                    }

                    while (podsWithFailingWatchdog.Count > WatchdogFailuresBeforeEviction)
                    {
                        podsWithFailingWatchdog.Dequeue();
                    }

                    if (podsWithFailingWatchdog.Count == WatchdogFailuresBeforeEviction)
                    {
                        var podsFailingEveryWatchdog = new HashSet<PodIdentifier>(thisIterationFailingPods);
                        foreach (var iteration in podsWithFailingWatchdog)
                        {
                            podsFailingEveryWatchdog.IntersectWith(iteration);
                        }

                        if (podsFailingEveryWatchdog.Any())
                        {
                            await this.EvictPods(podsFailingEveryWatchdog);
                        }
                    }

                    podsWithFailingWatchdog.Enqueue(thisIterationFailingPods);

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

        private Task EvictPods(HashSet<PodIdentifier> pods)
        {
            return Task.WhenAll(pods.Select(p => this.client.EvictPodAsync(p)).ToArray());
        }
    }
}
