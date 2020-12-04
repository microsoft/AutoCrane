﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Services
{
    internal sealed class CredentialProviderForAzureKeyVault : ICredentialProvider
    {
        private const string Protocol = "azkv";
        private static readonly TimeSpan SecretCacheTimeout = TimeSpan.FromMinutes(30);
        private readonly HttpClient client;
        private readonly ILogger<CredentialProviderForAzureKeyVault> logger;
        private readonly IClock clock;
        private readonly CredentialProviderForAzureManagedIdentity managedIdentity;
        private readonly ISecretCache secretCache;

        public CredentialProviderForAzureKeyVault(ILoggerFactory loggerFactory, IClock clock, CredentialProviderForAzureManagedIdentity managedIdentity, ISecretCache secretCache)
        {
            this.client = new HttpClient();
            this.logger = loggerFactory.CreateLogger<CredentialProviderForAzureKeyVault>();
            this.clock = clock;
            this.managedIdentity = managedIdentity;
            this.secretCache = secretCache;
            this.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public bool CanLookup(string credentialSpec)
        {
            return credentialSpec.StartsWith(Protocol + ":");
        }

        public async Task<SecretCredential> LookupAsync(string credentialSpec)
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

            var kvResourceUrl = "https://vault.azure.net";
            var miCredSpec = $"{CredentialProviderForAzureManagedIdentity.ProtocolName}{kvResourceUrl}";
            string secretToken;
            if (this.secretCache.TryGetValue(miCredSpec, out var cachedSecret))
            {
                secretToken = cachedSecret.Secret;
            }
            else
            {
                var accessToken = await this.managedIdentity.LookupAsync(miCredSpec);
                this.secretCache.TryAdd(miCredSpec, accessToken);
                secretToken = accessToken.Secret;
            }

            var requestUrl = $"https://{kvName}.vault.azure.net/secrets/{kvSecret}?api-version=7.0";
            this.logger.LogInformation($"GET {requestUrl}");
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secretToken);
            using var response = await this.client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var secretJson = await response.Content.ReadAsStringAsync();
            var value = ReadJsonValue(secretJson, "value");
            return new SecretCredential(value, this.clock.Get() + SecretCacheTimeout);
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
    }
}
