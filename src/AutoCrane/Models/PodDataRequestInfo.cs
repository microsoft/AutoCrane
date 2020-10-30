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
            this.Requests = annotations.Where(a => a.Key.StartsWith(CommonAnnotations.DataRequestPrefix)).ToDictionary(a => a.Key, a => a.Value);
            this.ActiveRequests = new List<PodDataRequest>();
            foreach (var ann in this.Requests)
            {
                var statusKey = ann.Key.Replace(CommonAnnotations.DataRequestPrefix, CommonAnnotations.DataStatusPrefix);

                // if the status annotation does not exist or if it's not the same as the request (meaning it's not complete)
                if (!annotations.TryGetValue(statusKey, out var status) || status != ann.Value)
                {
                    this.ActiveRequests.Add(new PodDataRequest(ann.Key.Replace(CommonAnnotations.DataRequestPrefix, string.Empty), ann.Value));
                }
            }
        }

        public PodIdentifier Id { get; }

        public IList<PodDataRequest> ActiveRequests { get; }

        public IReadOnlyDictionary<string, string> Requests { get; }
    }
}
