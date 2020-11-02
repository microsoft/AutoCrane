// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace AutoCrane.Models
{
    public class PodDataRequestInfo
    {
        public PodDataRequestInfo(PodIdentifier id, IReadOnlyDictionary<string, string> annotations)
        {
            this.Id = id;
            this.DataRepos = annotations.Where(a => a.Key.StartsWith(CommonAnnotations.DataDeploymentPrefix)).ToDictionary(a => a.Key.Replace(CommonAnnotations.DataDeploymentPrefix, string.Empty), a => a.Value);
            this.Requests = annotations.Where(a => a.Key.StartsWith(CommonAnnotations.DataRequestPrefix)).ToDictionary(a => a.Key.Replace(CommonAnnotations.DataRequestPrefix, string.Empty), a => a.Value);
            this.Status = annotations.Where(a => a.Key.StartsWith(CommonAnnotations.DataStatusPrefix)).ToDictionary(a => a.Key.Replace(CommonAnnotations.DataStatusPrefix, string.Empty), a => a.Value);

            var inProgressRequests = new List<PodDataRequest>();
            foreach (var ann in this.Requests)
            {
                var statusKey = ann.Key.Replace(CommonAnnotations.DataRequestPrefix, CommonAnnotations.DataStatusPrefix);

                // if the status annotation does not exist or if it's not the same as the request (meaning it's not complete)
                if (!annotations.TryGetValue(statusKey, out var status) || status != ann.Value)
                {
                    inProgressRequests.Add(new PodDataRequest(ann.Key, ann.Value));
                }
            }

            this.CompletedRequests = this.Requests.Where(r => this.Status.TryGetValue(r.Key, out var statusVal) && statusVal == r.Value).Select(r => r.Key).ToList();
            this.InProgressRequests = this.Requests.Where(r => !this.Status.TryGetValue(r.Key, out var statusVal) || statusVal != r.Value).Select(r => r.Key).ToList();
            this.NeedsRequest = this.DataRepos.Where(r => !this.Requests.ContainsKey(r.Key)).Select(r => r.Key).ToList();
        }

        public PodIdentifier Id { get; }

        public IReadOnlyList<string> InProgressRequests { get; }

        public IReadOnlyList<string> CompletedRequests { get; }

        public IReadOnlyList<string> NeedsRequest { get; }

        public IReadOnlyDictionary<string, string> DataRepos { get; }

        public IReadOnlyDictionary<string, string> Requests { get; }

        public IReadOnlyDictionary<string, string> Status { get; }
    }
}
