// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    internal sealed class KubernetesClient : IFailingPodGetter, IEndpointAnnotationAccessor, IPodGetter, IExpiredObjectDeleter
    {
        private readonly ILogger<KubernetesClient> logger;
        private readonly Kubernetes client;
        private readonly IWatchdogStatusAggregator statusAggregator;
        private readonly IAutoCraneConfig config;
        private readonly IDurationParser durationParser;

        public KubernetesClient(IKubernetesConfigProvider configProvider, IWatchdogStatusAggregator statusAggregator, ILoggerFactory loggerFactory, IAutoCraneConfig config, IDurationParser durationParser)
        {
            this.logger = loggerFactory.CreateLogger<KubernetesClient>();
            this.client = new Kubernetes(configProvider.Get());
            this.statusAggregator = statusAggregator;
            this.config = config;
            this.durationParser = durationParser;
        }

        public async Task<IReadOnlyDictionary<string, string>> GetEndpointAnnotationsAsync(string ns, string endpoint, CancellationToken token)
        {
            try
            {
                if (!this.config.IsAllowedNamespace(ns))
                {
                    throw new ForbiddenException($"namespace: {ns}");
                }

                var ep = await this.client.ReadNamespacedEndpointsAsync(endpoint, ns, cancellationToken: token);
                return (IReadOnlyDictionary<string, string>)ep.Annotations() ?? new Dictionary<string, string>();
            }
            catch (HttpOperationException e)
            {
                if (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new Dictionary<string, string>();
                }

                if (!string.IsNullOrEmpty(e.Response.Content))
                {
                    this.logger.LogError($"Exception response content: {e.Response.Content}");
                }

                throw;
            }
        }

        public async Task PutEndpointAnnotationsAsync(string ns, string endpoint, IReadOnlyDictionary<string, string> annotationsToUpdate, CancellationToken token)
        {
            V1Endpoints ep;
            try
            {
                if (!this.config.IsAllowedNamespace(ns))
                {
                    throw new ForbiddenException($"namespace: {ns}");
                }

                ep = await this.client.ReadNamespacedEndpointsAsync(endpoint, ns, cancellationToken: token);
            }
            catch (HttpOperationException e)
            {
                if (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    var newEp = new V1Endpoints()
                    {
                        Metadata = new V1ObjectMeta()
                        {
                            NamespaceProperty = ns,
                            Name = endpoint,
                        },
                    };

                    ep = await this.client.CreateNamespacedEndpointsAsync(newEp, ns, cancellationToken: token);
                }
                else
                {
                    this.logger.LogError($"Exception Getting LKG: {e.Response.Content}");
                    throw;
                }
            }

            try
            {
                var newannotations = new Dictionary<string, string>(ep.Annotations() ?? new Dictionary<string, string>());
                foreach (var ann in annotationsToUpdate)
                {
                    newannotations[ann.Key] = ann.Value;
                }

                var patch = new JsonPatchDocument<V1Endpoints>();
                patch.Replace(e => e.Metadata.Annotations, newannotations);
                var result = await this.client.PatchNamespacedEndpointsAsync(new V1Patch(patch, V1Patch.PatchType.JsonPatch), endpoint, ns, cancellationToken: token);
                Console.Error.WriteLine($"{result.Name()} updated");
            }
            catch (HttpOperationException e)
            {
                this.logger.LogError($"Exception Putting LKG: {e.Response.Content}");
                throw;
            }
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
                    return new PodInfo(podName, new Dictionary<string, string>(), this.ReadPodContainerState(existingPod), string.Empty);
                }

                return new PodInfo(podName, new Dictionary<string, string>(annotations), this.ReadPodContainerState(existingPod), existingPod.Status.PodIP);
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

        public async Task DeleteExpiredObjectsAsync(string ns, DateTimeOffset now, CancellationToken token)
        {
            try
            {
                if (!this.config.IsAllowedNamespace(ns))
                {
                    throw new ForbiddenException($"namespace: {ns}");
                }

                { // services
                    var serviceList = await this.client.ListNamespacedServiceAsync(ns, cancellationToken: token);
                    var listToDelete = serviceList.Items
                        .Where(x => x.Metadata.Annotations != null
                                    && x.Metadata.Annotations.TryGetValue("janitor/ttl", out var ttl)
                                    && this.TimeToLiveIsExpired(x.Metadata.CreationTimestamp, ttl, now))
                        .Select(x => x.Name())
                        .ToList();
                    foreach (var name in listToDelete)
                    {
                        this.logger.LogInformation($"Deleting Service {ns}/{name}", ns, name);
                        await this.client.DeleteNamespacedServiceAsync(name, ns, cancellationToken: token);
                    }
                }

                { // deployments
                    var serviceList = await this.client.ListNamespacedDeploymentAsync(ns, cancellationToken: token);
                    var listToDelete = serviceList.Items
                        .Where(x => x.Metadata.Annotations != null
                                    && x.Metadata.Annotations.TryGetValue("janitor/ttl", out var ttl)
                                    && this.TimeToLiveIsExpired(x.Metadata.CreationTimestamp, ttl, now))
                        .Select(x => x.Name())
                        .ToList();
                    foreach (var name in listToDelete)
                    {
                        this.logger.LogInformation($"Deleting Deployment {ns}/{name}", ns, name);
                        await this.client.DeleteNamespacedDeploymentAsync(name, ns, cancellationToken: token);
                    }
                }
            }
            catch (HttpOperationException e)
            {
                this.logger.LogError($"Exception Deleting Services: {e.Response.Content}");
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

                    list.Add(new PodInfo(new PodIdentifier(item.Namespace(), item.Name()), dict, this.ReadPodContainerState(item), item.Status.PodIP));
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

                var result = await this.client.PatchNamespacedPodAsync(new V1Patch(patch, V1Patch.PatchType.JsonPatch), podName.Name, podName.Namespace);
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

        private IReadOnlyDictionary<string, bool> ReadPodContainerState(V1Pod existingPod)
        {
            var dict = new Dictionary<string, bool>();
            if (existingPod == null)
            {
                return dict;
            }

            if (existingPod.Status.ContainerStatuses != null)
            {
                foreach (var item in existingPod.Status.ContainerStatuses)
                {
                    dict[item.Name] = item.Ready;
                }
            }

            if (existingPod.Status.EphemeralContainerStatuses != null)
            {
                foreach (var item in existingPod.Status.EphemeralContainerStatuses)
                {
                    dict[item.Name] = item.Ready;
                }
            }

            return dict;
        }

        private bool TimeToLiveIsExpired(DateTime? creation, string ttlString, DateTimeOffset now)
        {
            if (creation == null || creation == DateTime.MinValue)
            {
                return false;
            }

            var ttl = this.durationParser.Parse(ttlString);
            if (ttl == null)
            {
                return false;
            }

            if (creation + ttl < now)
            {
                return true;
            }

            return false;
        }
    }
}
