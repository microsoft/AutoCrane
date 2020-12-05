// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using AutoCrane.Models;

namespace AutoCrane.Interfaces
{
    public interface ISecretCache
    {
        bool TryGetValue(string credentialSpec, out SecretCredential secret);

        void TryAdd(string credentialSpec, SecretCredential credential);
    }
}
