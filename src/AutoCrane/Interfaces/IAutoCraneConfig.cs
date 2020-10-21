// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace AutoCrane.Interfaces
{
    public interface IAutoCraneConfig
    {
        IEnumerable<string> Namespaces { get; }

        long EvictionDeleteGracePeriodSeconds { get; }

        bool IsAllowedNamespace(string ns);
    }
}
