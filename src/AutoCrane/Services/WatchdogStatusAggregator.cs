// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using AutoCrane.Interfaces;
using AutoCrane.Models;

namespace AutoCrane.Services
{
    internal sealed class WatchdogStatusAggregator : IWatchdogStatusAggregator
    {
        public string Aggregate(IDictionary<string, string> annotations)
        {
            var maxStatus = "Unknown";
            foreach (var annotation in annotations)
            {
                if (annotation.Key.StartsWith(WatchdogStatus.Prefix))
                {
                    var idx = annotation.Value.IndexOf('/');
                    if (idx > 0)
                    {
                        maxStatus = this.MoreCriticalStatus(maxStatus, annotation.Value.Substring(0, idx));
                    }
                }
            }

            return maxStatus;
        }

        public string MoreCriticalStatus(string s1, string s2)
        {
            var s1v = StatusWeight(s1);
            var s2v = StatusWeight(s2);

            if (s1v >= s2v)
            {
                return s1;
            }

            return s2;
        }

        private static int StatusWeight(string s2)
        {
            return s2.ToLowerInvariant() switch
            {
                "error" => 3,
                "warning" => 2,
                "info" => 1,
                _ => 0,
            };
        }
    }
}
