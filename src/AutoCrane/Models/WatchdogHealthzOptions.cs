// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace AutoCrane.Models
{
    public sealed class WatchdogHealthzOptions
    {
        /// <summary>
        /// The minimum seconds before we can consider this healthy.
        /// </summary>
        public int? MinReadySeconds { get; set; }

        /// <summary>
        /// The number of seconds before we are always healthy. Used if we don't want a readiness probe to take this pod out of service.
        /// </summary>
        public int? AlwaysHealthyAfterSeconds { get; set; }
    }
}
