// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace AutoCrane.Interfaces
{
    public interface IWatchdogStatusAggregator
    {
        /// <summary>
        /// Compute the overall health from looking at all the watchdog status levels. Should be the worse one.
        /// </summary>
        /// <param name="annotations">the pod annotations.</param>
        /// <returns>the worst status.</returns>
        string Aggregate(IReadOnlyDictionary<string, string> annotations);

        /// <summary>
        /// Compares and returns the worse (more critical) status of the two.
        /// </summary>
        /// <param name="s1">first status.</param>
        /// <param name="s2">second status.</param>
        /// <returns>the more critical status.</returns>
        string MoreCriticalStatus(string s1, string s2);
    }
}
