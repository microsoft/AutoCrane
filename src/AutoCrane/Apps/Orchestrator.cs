// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Apps
{
    public sealed class Orchestrator : IAutoCraneService
    {
        private const int ConsecutiveErrorCountBeforeExiting = 5;
        private const int IterationLoopSeconds = 60;
        private const int WatchdogFailuresBeforeEviction = 3;
        private readonly IAutoCraneConfig config;
        private readonly IFailingPodGetter failingPodGetter;
        private readonly IPodEvicter podEvicter;
        private readonly IPodDataRequestGetter dataRequestGetter;
        private readonly IDataRepositoryManifestFetcher manifestFetcher;
        private readonly IPodAnnotationPutter podAnnotationPutter;
        private readonly IDataRepositoryKnownGoodAccessor knownGoodAccessor;
        private readonly ILogger<Orchestrator> logger;

        public Orchestrator(IAutoCraneConfig config, ILoggerFactory loggerFactory, IFailingPodGetter failingPodGetter, IPodEvicter podEvicter, IPodDataRequestGetter podGetter, IDataRepositoryManifestFetcher manifestFetcher, IPodAnnotationPutter podAnnotationPutter, IDataRepositoryKnownGoodAccessor knownGoodAccessor)
        {
            this.config = config;
            this.failingPodGetter = failingPodGetter;
            this.podEvicter = podEvicter;
            this.dataRequestGetter = podGetter;
            this.manifestFetcher = manifestFetcher;
            this.podAnnotationPutter = podAnnotationPutter;
            this.knownGoodAccessor = knownGoodAccessor;
            this.logger = loggerFactory.CreateLogger<Orchestrator>();
        }

        public async Task<int> RunAsync(CancellationToken token)
        {
            var iterations = int.MaxValue;
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
                    token.ThrowIfCancellationRequested();

                    var manifest = await this.manifestFetcher.FetchAsync(token);

                    var thisIterationFailingPods = new List<PodIdentifier>();
                    foreach (var ns in this.config.Namespaces)
                    {
                        var lkg = await this.knownGoodAccessor.GetOrCreateAsync(ns, manifest, token);
                        var requests = await this.dataRequestGetter.GetAsync(ns);
                        await this.ProcessDataRequestsAsync(lkg, requests);

                        if (lkg.OngoingUpdates.Any())
                        {

                        }

                        var failingPods = await this.failingPodGetter.GetFailingPodsAsync(ns);
                        thisIterationFailingPods.AddRange(failingPods);
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

                    await Task.Delay(TimeSpan.FromSeconds(IterationLoopSeconds), token);
                    iterations--;
                    errorCount = 0;
                }
                catch (Exception e)
                {
                    this.logger.LogError($"Unhandled exception: {e}");
                    errorCount++;
                    await Task.Delay(TimeSpan.FromSeconds(IterationLoopSeconds), token);
                }
            }

            return 0;
        }

        private async Task ProcessDataRequestsAsync(DataRepositoryKnownGoods lkg, IReadOnlyList<PodDataRequestInfo> requests)
        {
            foreach (var podRequest in requests.Where(r => r.NeedsRequest.Any()))
            {
                var annotationsToAdd = new List<KeyValuePair<string, string>>();
                foreach (var request in podRequest.NeedsRequest)
                {
                    if (podRequest.DataRepos.TryGetValue(request, out var dataRepoSpec))
                    {
                        if (lkg.KnownGoodVersions.TryGetValue(dataRepoSpec, out var requestSpec))
                        {
                            this.logger.LogInformation($"Pod {podRequest.Id} requesting initial data {dataRepoSpec}, got LKG {requestSpec}");
                            annotationsToAdd.Add(new KeyValuePair<string, string>($"{CommonAnnotations.DataRequestPrefix}{request}", requestSpec));
                        }
                        else
                        {
                            this.logger.LogError($"Pod {podRequest.Id} is requesting data repo {dataRepoSpec} which is not found in LKG sources: {string.Join(',', lkg.KnownGoodVersions.Keys)}");
                        }
                    }
                    else
                    {
                        // set annotation?
                        this.logger.LogError($"Pod {podRequest.Id} is missing annotation {CommonAnnotations.DataDeploymentPrefix}/{request}");
                    }
                }

                if (annotationsToAdd.Any())
                {
                    await this.podAnnotationPutter.PutPodAnnotationAsync(podRequest.Id, annotationsToAdd);
                }
            }
        }

        private async Task<IReadOnlyList<PodDataRequestInfo>> FetchDataRequestsAsync(IEnumerable<string> namespaces)
        {
            var list = new List<PodDataRequestInfo>();
            foreach (var ns in namespaces)
            {
                list.AddRange(await this.dataRequestGetter.GetAsync(ns));
            }

            return list;
        }

        private Task EvictPods(HashSet<PodIdentifier> pods)
        {
            return Task.WhenAll(pods.Select(p => this.podEvicter.EvictPodAsync(p)).ToArray());
        }
    }
}
