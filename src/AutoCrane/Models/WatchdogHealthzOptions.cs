// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace AutoCrane.Models
{
    public sealed class WatchdogHealthzOptions
    {
        public int? RequireHealthyStatusForSeconds { get; set; }

        public int? AlwaysHealthyAfterSeconds { get; set; }
    }
}
