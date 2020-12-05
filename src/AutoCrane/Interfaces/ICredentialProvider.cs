// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading.Tasks;
using AutoCrane.Models;

namespace AutoCrane.Interfaces
{
    public interface ICredentialProvider
    {
        bool CanLookup(string credentialSpec);

        Task<SecretCredential> LookupAsync(string credentialSpec, ICredentialHelper credentialHelper);
    }
}
