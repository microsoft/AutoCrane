// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoCrane.Interfaces;

namespace AutoCrane.Services
{
    internal sealed class CredentialHelper : ICredentialHelper
    {
        private readonly IEnumerable<ICredentialProvider> providers;

        public CredentialHelper(IEnumerable<ICredentialProvider> providers)
        {
            this.providers = providers;
        }

        public Task<string> LookupAsync(string credentialSpec)
        {
            var provider = this.providers.Where(p => p.CanLookup(credentialSpec)).FirstOrDefault();
            if (provider == null)
            {
                throw new ArgumentOutOfRangeException(nameof(credentialSpec));
            }

            return provider.LookupAsync(credentialSpec);
        }
    }
}
