// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Services
{
    internal sealed class SecretCache : ISecretCache
    {
        private readonly ConcurrentDictionary<string, SecretCredential> secretCache;
        private readonly ILogger<SecretCache> logger;
        private readonly IClock clock;

        public SecretCache(ILoggerFactory loggerFactory, IClock clock)
        {
            this.secretCache = new ConcurrentDictionary<string, SecretCredential>();
            this.logger = loggerFactory.CreateLogger<SecretCache>();
            this.clock = clock;
        }

        public void TryAdd(string credentialSpec, SecretCredential newVal)
        {
            this.secretCache.AddOrUpdate(credentialSpec, newVal, (_, _) => newVal);
        }

        public bool TryGetValue(string credentialSpec, out SecretCredential secret)
        {
            if (this.secretCache.TryGetValue(credentialSpec, out var cachedValue))
            {
                var now = this.clock.Get();
                if (now < cachedValue.Expiry - TimeSpan.FromMinutes(5))
                {
                    this.logger.LogInformation($"Found cached token for {credentialSpec}, expires after {cachedValue.Expiry}");
                    secret = cachedValue;
                    return true;
                }
                else
                {
                    this.logger.LogInformation($"Found cached token for {credentialSpec}, but expired already or near it ({cachedValue.Expiry})");
                    secret = SecretCredential.Empty;
                    return false;
                }
            }

            secret = SecretCredential.Empty;
            return false;
        }
    }
}
