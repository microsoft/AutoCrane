// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using AutoCrane.Models;

namespace AutoCrane.Interfaces
{
    public interface IPodIdentifierFactory
    {
        PodIdentifier FromString(string id);

        PodIdentifier FromString(string ns, string id);
    }
}
