// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.LeaderElection.ResourceLock;
using k8s.Models;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Services
{
    internal class AutoCraneEndpointsLock : MetaObjectAnnotationLock<V1Endpoints>
    {
        private readonly ILogger logger;

        public AutoCraneEndpointsLock(IKubernetes client, string @namespace, string name, string identity, ILogger logger)
            : base(client, @namespace, name, identity)
        {
            this.logger = logger;
        }

        protected override Task<V1Endpoints> ReadMetaObjectAsync(IKubernetes client, string name, string namespaceParameter, CancellationToken cancellationToken)
        {
            try
            {
                return client.ReadNamespacedEndpointsAsync(name, namespaceParameter, cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                this.logger.LogError($"Unhandled exception in ReadMetaObjectAsync: {e}");
                throw;
            }
        }

        protected override Task<V1Endpoints> CreateMetaObjectAsync(IKubernetes client, V1Endpoints obj, string namespaceParameter, CancellationToken cancellationToken)
        {
            try
            {
                return client.CreateNamespacedEndpointsAsync(obj, namespaceParameter, cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                this.logger.LogError($"Unhandled exception in CreateMetaObjectAsync: {e}");
                throw;
            }
        }

        protected override Task<V1Endpoints> ReplaceMetaObjectAsync(IKubernetes client, V1Endpoints obj, string name, string namespaceParameter, CancellationToken cancellationToken)
        {
            try
            {
                return client.ReplaceNamespacedEndpointsAsync(obj, name, namespaceParameter, cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                this.logger.LogError($"Unhandled exception in ReplaceNamespacedEndpointsAsync: {e}");
                throw;
            }
        }
    }
}
