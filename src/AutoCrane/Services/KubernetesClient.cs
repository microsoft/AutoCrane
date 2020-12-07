// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoCrane.Exceptions;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using k8s;
using k8s.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;

namespace AutoCrane.Services
{
    internal sealed class KubernetesClient
    {
        private readonly ILogger<KubernetesClient> logger;
        private readonly Kubernetes client;
        private readonly IWatchdogStatusAggregator statusAggregator;
        private readonly IAutoCraneConfig config;

        public KubernetesClient(IKubernetesConfigProvider configProvider, IWatchdogStatusAggregator statusAggregator, ILoggerFactory loggerFactory, IAutoCraneConfig config)
        {
            this.logger = loggerFactory.CreateLogger<KubernetesClient>();
            this.client = new Kubernetes(configProvider.Get());
            this.statusAggregator = statusAggregator;
            this.config = config;
        }

        public async Task EvictPodAsync(PodIdentifier p)
        {
            try
            {
                if (!this.config.IsAllowedNamespace(p.Namespace))
                {
                    throw new ForbiddenException($"namespace: {p.Namespace}");
                }

                this.logger.LogInformation($"Evicting pod {p.Name} in {p.Namespace}");
                var body = new V1beta1Eviction()
                {
                    Metadata = new V1ObjectMeta(namespaceProperty: p.Namespace, name: p.Name),
                    DeleteOptions = new V1DeleteOptions(gracePeriodSeconds: this.config.EvictionDeleteGracePeriodSeconds),
                };

                await this.client.CreateNamespacedPodEvictionAsync(body, p.Name, p.Namespace);
            }
            catch (HttpOperationException e)
            {
                if (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return;
                }

                if (!string.IsNullOrEmpty(e.Response.Content))
                {
                    this.logger.LogError($"Exception response content: {e.Response.Content}");
                }

                throw;
            }
        }

        public Task<LeaderElectionResults> ElectLeaderAsync(string election)
        {
            throw new NotImplementedException();
        }

        public async Task<IReadOnlyList<PodIdentifier>> GetFailingPodsAsync(string ns)
        {
            try
            {
                if (!this.config.IsAllowedNamespace(ns))
                {
                    throw new ForbiddenException($"namespace: {ns}");
                }

                var list = await this.client.ListNamespacedPodAsync(ns, labelSelector: $"{WatchdogStatus.Prefix}health=error");
                return list.Items.Select(li => new PodIdentifier(ns, li.Name())).ToList();
            }
            catch (HttpOperationException e)
            {
                if (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return Array.Empty<PodIdentifier>();
                }

                if (!string.IsNullOrEmpty(e.Response.Content))
                {
                    this.logger.LogError($"Exception response content: {e.Response.Content}");
                }

                throw;
            }
        }

        public async Task<PodInfo> GetPodAnnotationAsync(PodIdentifier podName)
        {
            try
            {
                var existingPod = await this.client.ReadNamespacedPodAsync(podName.Name, podName.Namespace);
                var annotations = existingPod.Annotations();
                if (annotations == null)
                {
                    this.logger.LogError($"Annotations is null");
                    return new PodInfo(podName, new Dictionary<string, string>(), string.Empty);
                }

                return new PodInfo(podName, new Dictionary<string, string>(annotations), existingPod.Status.PodIP);
            }
            catch (HttpOperationException e)
            {
                if (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new PodNotFoundException(podName);
                }

                if (!string.IsNullOrEmpty(e.Response.Content))
                {
                    this.logger.LogError($"Exception response content: {e.Response.Content}");
                }

                throw;
            }
        }

        public async Task<IReadOnlyList<PodInfo>> GetPodAnnotationAsync(string ns)
        {
            try
            {
                var podList = await this.client.ListNamespacedPodAsync(ns);
                var list = new List<PodInfo>(podList.Items.Count);
                foreach (var item in podList.Items)
                {
                    var ann = item.Annotations();
                    IReadOnlyDictionary<string, string> dict;
                    if (ann == null)
                    {
                        dict = new Dictionary<string, string>();
                    }
                    else
                    {
                        dict = new Dictionary<string, string>(ann);
                    }

                    list.Add(new PodInfo(new PodIdentifier(item.Namespace(), item.Name()), dict, item.Status.PodIP));
                }

                return list;
            }
            catch (HttpOperationException e)
            {
                if (!string.IsNullOrEmpty(e.Response.Content))
                {
                    this.logger.LogError($"Exception response content: {e.Response.Content}");
                }

                throw;
            }
        }

        public Task PutPodAnnotationAsync(PodIdentifier podName, string name, string val, bool updateHealth = true)
        {
            return this.PutPodAnnotationAsync(podName, new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>(name, val) }, updateHealth);
        }

        public async Task PutPodAnnotationAsync(PodIdentifier podName, IReadOnlyList<KeyValuePair<string, string>> annotationsToUpdate, bool updateHealth = true)
        {
            try
            {
                if (!this.config.IsAllowedNamespace(podName.Namespace))
                {
                    throw new ForbiddenException($"namespace: {podName.Namespace}");
                }

                var existingPod = await this.client.ReadNamespacedPodAsync(podName.Name, podName.Namespace);
                var newannotations = new Dictionary<string, string>(existingPod.Annotations() ?? new Dictionary<string, string>());
                foreach (var ann in annotationsToUpdate)
                {
                    newannotations[ann.Key] = ann.Value;
                }

                var patch = new JsonPatchDocument<V1Pod>();
                patch.Replace(e => e.Metadata.Annotations, newannotations);
                if (updateHealth)
                {
                    var newlabels = new Dictionary<string, string>(existingPod.Labels())
                    {
                        [$"{WatchdogStatus.Prefix}health"] = this.statusAggregator.Aggregate(newannotations),
                    };
                    patch.Replace(e => e.Metadata.Labels, newlabels);
                }

                var result = await this.client.PatchNamespacedPodAsync(new V1Patch(patch), podName.Name, podName.Namespace);
                Console.Error.WriteLine($"{result.Name()} updated");
            }
            catch (HttpOperationException e)
            {
                if (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new PodNotFoundException(podName);
                }

                throw;
            }
        }
    }
}
