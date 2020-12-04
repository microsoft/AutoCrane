// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;

namespace AutoCrane.Services
{
    internal sealed class CredentialHelper : ICredentialHelper
    {
        private readonly IEnumerable<ICredentialProvider> providers;
        private readonly ISecretCache secretCache;

        public CredentialHelper(IEnumerable<ICredentialProvider> providers, ISecretCache secretCache)
        {
            this.providers = providers;
            this.secretCache = secretCache;
        }

        public async Task<SecretCredential> LookupAsync(string credentialSpec)
        {
            var provider = this.providers.Where(p => p.CanLookup(credentialSpec)).FirstOrDefault();
            if (provider == null)
            {
                throw new ArgumentOutOfRangeException(nameof(credentialSpec));
            }

            if (this.secretCache.TryGetValue(credentialSpec, out var secret))
            {
                return secret;
            }

            var result = await provider.LookupAsync(credentialSpec);

            this.secretCache.TryAdd(credentialSpec, result);

            return result;
        }
    }
}
