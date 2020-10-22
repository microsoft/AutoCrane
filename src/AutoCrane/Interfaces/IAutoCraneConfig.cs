// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace AutoCrane.Interfaces
{
    public interface IAutoCraneConfig
    {
        IEnumerable<string> Namespaces { get; }

        long EvictionDeleteGracePeriodSeconds { get; }

        TimeSpan WatchdogProbeTimeout { get; }

        bool IsAllowedNamespace(string ns);
    }
}
