// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace AutoCrane.Models
{
    public sealed class AutoCraneOptions
    {
        public string? Namespaces { get; set; }

        public int? EvictionDeleteGracePeriodSeconds { get; set; }

        public int? WatchdogProbeTimeoutSeconds { get; set; }
    }
}
