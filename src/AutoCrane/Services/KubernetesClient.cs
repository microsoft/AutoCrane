// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using AutoCrane.Exceptions;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using k8s;
using k8s.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Rest;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace AutoCrane.Services
{
    internal sealed class KubernetesClient : IKubernetesClient
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

        public async Task<IReadOnlyList<string>> GetFailingPodsAsync(string ns)
        {
            try
            {
                if (!this.config.IsAllowedNamespace(ns))
                {
                    throw new ForbiddenException($"namespace: {ns}");
                }

                var list = await this.client.ListNamespacedPodAsync(ns, labelSelector: $"{WatchdogStatus.Prefix}health=error");
                return list.Items.Select(li => li.Name()).ToList();
            }
            catch (HttpOperationException e)
            {
                if (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return Array.Empty<string>();
                }

                if (!string.IsNullOrEmpty(e.Response.Content))
                {
                    this.logger.LogError($"Exception response content: {e.Response.Content}");
                }

                throw;
            }
        }

        public async Task<IReadOnlyDictionary<string, string>> GetPodAnnotationAsync(PodIdentifier podName)
        {
            try
            {
                if (!this.config.IsAllowedNamespace(podName.Namespace))
                {
                    throw new ForbiddenException($"namespace: {podName.Namespace}");
                }

                this.logger.LogInformation($"ReadNamespacedPodAsync {podName.Namespace} {podName.Name}");
                var existingPod = await this.client.ReadNamespacedPodAsync(podName.Name, podName.Namespace);
                var annotations = existingPod.Annotations();
                if (annotations == null)
                {
                    this.logger.LogError($"Annotations is null");
                    return new Dictionary<string, string>();
                }

                return annotations.Where(a => a.Key?.StartsWith(WatchdogStatus.Prefix) ?? false).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
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

        public async Task PutPodAnnotationAsync(PodIdentifier podName, string name, string val)
        {
            try
            {
                if (!this.config.IsAllowedNamespace(podName.Namespace))
                {
                    throw new ForbiddenException($"namespace: {podName.Namespace}");
                }

                var existingPod = await this.client.ReadNamespacedPodAsync(podName.Name, podName.Namespace);
                var newannotations = new Dictionary<string, string>(existingPod.Annotations() ?? new Dictionary<string, string>())
                {
                    [name] = val,
                };

                var newlabels = new Dictionary<string, string>(existingPod.Labels())
                {
                    [$"{WatchdogStatus.Prefix}health"] = this.statusAggregator.Aggregate(newannotations),
                };

                var patch = new JsonPatchDocument<V1Pod>();
                patch.Replace(e => e.Metadata.Annotations, newannotations);
                patch.Replace(e => e.Metadata.Labels, newlabels);
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
