// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Services
{
    internal sealed class CredentialProviderForAzureManagedIdentity : ICredentialProvider
    {
        private const string Protocol = "azmi";
        private readonly HttpClient client;
        private readonly ILogger<CredentialProviderForAzureManagedIdentity> logger;
        private readonly IClock clock;

        public CredentialProviderForAzureManagedIdentity(ILoggerFactory loggerFactory, IClock clock)
        {
            this.client = new HttpClient();
            this.logger = loggerFactory.CreateLogger<CredentialProviderForAzureManagedIdentity>();
            this.clock = clock;
        }

        public static string ProtocolName => Protocol;

        public bool CanLookup(string credentialSpec)
        {
            return credentialSpec.StartsWith(Protocol + ":");
        }

        public async Task<SecretCredential> LookupAsync(string credentialSpec, ICredentialHelper credentialHelper)
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

            var url = $"http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource={resource}";
            this.logger.LogInformation($"GET {url}");
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Metadata", "true");
            using var response = await this.client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var tokenJson = await response.Content.ReadAsStringAsync();
            var accessToken = ReadJsonValue(tokenJson, "access_token");
            var expiresInString = ReadJsonValue(tokenJson, "expires_in");
            var expiresInSeconds = long.Parse(expiresInString);
            var expiry = TimeSpan.FromSeconds(expiresInSeconds);
            return new SecretCredential(accessToken, this.clock.Get() + expiry);
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
