// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;

namespace AutoCrane.Services
{
    internal sealed class ExpiredObjectDeleter : IExpiredObjectDeleter
    {
        private readonly IClock clock;
        private readonly KubernetesClient client;

        public ExpiredObjectDeleter(IClock clock, KubernetesClient client)
        {
            this.clock = clock;
            this.client = client;
        }

        public Task DeleteAsync(string ns, CancellationToken token)
        {
            return this.client.DeleteExpiredObjects(ns, this.clock.Get(), token);
        }
    }
}
