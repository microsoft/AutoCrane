// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using AutoCrane.Interfaces;

namespace AutoCrane.Services
{
    internal sealed class CredentialProviderForEnvironmentVariables : ICredentialProvider
    {
        private const string EnvSecret = "env";

        public bool CanLookup(string credentialSpec)
        {
            return credentialSpec.StartsWith(EnvSecret + ":");
        }

        public Task<string> LookupAsync(string credentialSpec)
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

            return Task.FromResult(Environment.GetEnvironmentVariable(url) ?? string.Empty);
        }
    }
}
