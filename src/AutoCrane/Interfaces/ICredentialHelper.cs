// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading.Tasks;

namespace AutoCrane.Interfaces
{
    public interface ICredentialHelper
    {
        Task<string> LookupAsync(string credentialSpec);
    }
}
