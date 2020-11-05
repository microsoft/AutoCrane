// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
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
        private readonly HttpClient client;
        private readonly ILogger<CredentialProviderForAzureKeyVault> logger;
        private readonly CredentialProviderForAzureManagedIdentity managedIdentity;

        public CredentialProviderForAzureKeyVault(ILoggerFactory loggerFactory, CredentialProviderForAzureManagedIdentity managedIdentity)
        {
            this.client = new HttpClient();
            this.logger = loggerFactory.CreateLogger<CredentialProviderForAzureKeyVault>();
            this.managedIdentity = managedIdentity;
            this.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
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

            var kvResourceUrl = "https://vault.azure.net";
            var kvAccessTokenJson = await this.managedIdentity.RequestManagedIdentityTokenAsync(kvResourceUrl);
            var accessToken = ReadJsonValue(kvAccessTokenJson, "access_token");

            var requestUrl = $"https://{kvName}.vault.azure.net/secrets/{kvSecret}?api-version=7.0";
            this.logger.LogInformation($"GET {requestUrl}");
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await this.client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var secretJson = await response.Content.ReadAsStringAsync();
            return ReadJsonValue(secretJson, "value");
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
