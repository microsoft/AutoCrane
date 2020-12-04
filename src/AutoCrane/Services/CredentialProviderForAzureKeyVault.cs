// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Services
{
    internal sealed class CredentialProviderForAzureKeyVault : ICredentialProvider
    {
        private const string Protocol = "azkv";
        private static readonly TimeSpan SecretCacheTimeout = TimeSpan.FromMinutes(30);
        private readonly HttpClient client;
        private readonly ILogger<CredentialProviderForAzureKeyVault> logger;
        private readonly CredentialProviderForAzureManagedIdentity managedIdentity;
        private readonly IClock clock;
        private readonly ConcurrentDictionary<string, (string, DateTimeOffset)> secretCache;

        public CredentialProviderForAzureKeyVault(ILoggerFactory loggerFactory, CredentialProviderForAzureManagedIdentity managedIdentity, IClock clock)
        {
            this.client = new HttpClient();
            this.logger = loggerFactory.CreateLogger<CredentialProviderForAzureKeyVault>();
            this.managedIdentity = managedIdentity;
            this.clock = clock;
            this.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            this.secretCache = new ConcurrentDictionary<string, (string, DateTimeOffset)>();
        }

        public bool CanLookup(string credentialSpec)
        {
            return credentialSpec.StartsWith(Protocol + ":");
        }

        public async Task<string> LookupAsync(string credentialSpec)
        {
            var specSplits = credentialSpec.Split(':', 3);
            if (specSplits.Length != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(credentialSpec));
            }

            var spec = specSplits[0];
            var kvName = specSplits[1];
            var kvSecret = specSplits[2];
            if (spec != Protocol)
            {
                throw new NotImplementedException($"spec {spec} not {Protocol}");
            }

            if (this.secretCache.TryGetValue(credentialSpec, out var cachedValue))
            {
                (var cachedSecret, var cachedTime) = cachedValue;
                var now = this.clock.Get();
                var cacheAge = now - cachedTime;
                if (cacheAge < SecretCacheTimeout)
                {
                    this.logger.LogInformation($"Found cached secret, Cache age {cacheAge} < timeout {SecretCacheTimeout}");
                    return cachedSecret;
                }
                else
                {
                    this.logger.LogInformation($"Found expired cached secret, Cache age {cacheAge} > timeout {SecretCacheTimeout}");
                }
            }

            var kvResourceUrl = "https://vault.azure.net";
            var accessToken = await this.managedIdentity.RequestManagedIdentityTokenAsync(kvResourceUrl);

            var requestUrl = $"https://{kvName}.vault.azure.net/secrets/{kvSecret}?api-version=7.0";
            this.logger.LogInformation($"GET {requestUrl}");
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await this.client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var secretJson = await response.Content.ReadAsStringAsync();
            var value = ReadJsonValue(secretJson, "value");
            this.CacheSecret(credentialSpec, value);
            return value;
        }

        private static string ReadJsonValue(string jsonString, string property)
        {
            using var json = JsonDocument.Parse(jsonString);
            string? val = null;
            foreach (var prop in json.RootElement.EnumerateObject())
            {
                if (prop.Name == property)
                {
                    val = prop.Value.GetString();
                    break;
                }
            }

            if (val is null)
            {
                throw new InvalidDataException($"Cannot read property '{property}' in json string.");
            }

            return val;
        }

        private void CacheSecret(string credentialSpec, string value)
        {
            var newVal = (value, this.clock.Get());
            this.secretCache.AddOrUpdate(credentialSpec, newVal, (_, _) => newVal);
        }
    }
}
