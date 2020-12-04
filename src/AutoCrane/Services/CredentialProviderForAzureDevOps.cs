// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    internal sealed class CredentialProviderForAzureDevOps : ICredentialProvider
    {
        private const string Protocol = "adosp";
        private readonly HttpClient client;
        private readonly ILogger<CredentialProviderForAzureDevOps> logger;
        private readonly IClock clock;

        public CredentialProviderForAzureDevOps(ILoggerFactory loggerFactory, IClock clock)
        {
            this.client = new HttpClient();
            this.logger = loggerFactory.CreateLogger<CredentialProviderForAzureDevOps>();
            this.clock = clock;
            this.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public bool CanLookup(string credentialSpec)
        {
            return credentialSpec.StartsWith(Protocol + ":");
        }

        public async Task<SecretCredential> LookupAsync(string credentialSpec, ICredentialHelper credentialHelper)
        {
            var specSplits = credentialSpec.Split(':', 4);
            if (specSplits.Length != 4)
            {
                throw new ArgumentOutOfRangeException(nameof(credentialSpec));
            }

            var spec = specSplits[0];
            var resource = specSplits[1];
            var clientId = specSplits[2];
            var clientSecretSpec = specSplits[3];
            if (spec != Protocol)
            {
                throw new NotImplementedException($"spec {spec} not {Protocol}");
            }

            var clientSecret = await credentialHelper.LookupAsync(clientSecretSpec);

            var requestUrl = $"https://app.vssps.visualstudio.com/oauth2/token";
            this.logger.LogInformation($"POST {requestUrl}");
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            var postBody = new List<KeyValuePair<string?, string?>>()
            {
                new KeyValuePair<string?, string?>("grant_type", "client_credentials"),
                new KeyValuePair<string?, string?>("scope", "vso.code"),
                new KeyValuePair<string?, string?>("client_id", clientId),
                new KeyValuePair<string?, string?>("client_secret", clientSecret.Secret),
                new KeyValuePair<string?, string?>("resource", resource),
            };

            request.Content = new FormUrlEncodedContent(postBody);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("applicaiton/json"));
            using var response = await this.client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var secretJson = await response.Content.ReadAsStringAsync();
            var accessToken = ReadJsonValue(secretJson, "access_token");
            var expiresInString = ReadJsonValue(secretJson, "expires_in");
            var expiresIn = long.Parse(expiresInString);
            var expires = this.clock.Get() + TimeSpan.FromSeconds(expiresIn);
            return new SecretCredential(accessToken, expires);
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
