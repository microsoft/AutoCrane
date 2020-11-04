// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Services
{
    internal sealed class CredentialProviderForAzureManagedIdentity : ICredentialProvider
    {
        private const string Protocol = "azmi";
        private readonly HttpClient client;
        private readonly ILogger<CredentialProviderForAzureManagedIdentity> logger;

        public CredentialProviderForAzureManagedIdentity(ILoggerFactory loggerFactory)
        {
            this.client = new HttpClient();
            this.logger = loggerFactory.CreateLogger<CredentialProviderForAzureManagedIdentity>();
        }

        public static string ProtocolName => Protocol;

        public bool CanLookup(string credentialSpec)
        {
            return credentialSpec.StartsWith(Protocol + ":");
        }

        public Task<string> LookupAsync(string credentialSpec)
        {
            var specAndUrl = credentialSpec.Split(':', 2);
            if (specAndUrl.Length != 2)
            {
                throw new ArgumentOutOfRangeException(nameof(credentialSpec));
            }

            var spec = specAndUrl[0];
            var resource = specAndUrl[1];
            if (spec != Protocol)
            {
                throw new NotImplementedException($"spec {spec} not {Protocol}");
            }

            return this.RequestManagedIdentityTokenAsync(resource);
        }

        public async Task<string> RequestManagedIdentityTokenAsync(string resource)
        {
            var url = "http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource={resource}";
            this.logger.LogInformation($"GET {url}");
            using var request = new HttpRequestMessage(HttpMethod.Get, $"http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource={resource}");
            request.Headers.Add("Metadata", "true");
            using var response = await this.client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var tokenJson = await response.Content.ReadAsStringAsync();
            return tokenJson;
        }
    }
}
