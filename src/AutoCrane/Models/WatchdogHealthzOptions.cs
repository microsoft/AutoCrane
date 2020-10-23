// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace AutoCrane.Models
{
    public sealed class WatchdogHealthzOptions
    {
        /// <summary>
        /// Gets how long you need all successful watchdogs for before returning healthy.
        /// </summary>
        public int? RequireHealthyStatusForSeconds { get; }
    }
}
