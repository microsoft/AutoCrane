// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;

namespace AutoCrane.Services
{
    internal sealed class CredentialProviderForEnvironmentVariables : ICredentialProvider
    {
        private const string EnvSecret = "env";

        public bool CanLookup(string credentialSpec)
        {
            return credentialSpec.StartsWith(EnvSecret + ":");
        }

        public Task<SecretCredential> LookupAsync(string credentialSpec)
        {
            var specAndUrl = credentialSpec.Split(':', 2);
            if (specAndUrl.Length != 2)
            {
                throw new ArgumentOutOfRangeException(nameof(credentialSpec));
            }

            var spec = specAndUrl[0];
            var url = specAndUrl[1];
            if (spec != EnvSecret)
            {
                throw new NotImplementedException($"spec {spec} not {EnvSecret}");
            }

            // expire very far in the future (using MaxValue could cause unintended overflows)
            return Task.FromResult(new SecretCredential(Environment.GetEnvironmentVariable(url) ?? string.Empty, DateTimeOffset.FromUnixTimeSeconds(4_000_000_000)));
        }
    }
}
