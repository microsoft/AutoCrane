// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace AutoCrane.Interfaces
{
    public interface ICredentialProvider : ICredentialHelper
    {
        bool CanLookup(string credentialSpec);
    }
}
