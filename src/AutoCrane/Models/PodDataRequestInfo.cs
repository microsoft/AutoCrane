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
            this.DropFolder = annotations.FirstOrDefault(pi => pi.Key == CommonAnnotations.DataStoreLocation).Value ?? string.Empty;
            this.NeedsRequest = this.DataRepos.Where(r => !this.Requests.ContainsKey(r.Key)).Select(r => r.Key).ToList();
        }

        public PodIdentifier Id { get; }

        public IReadOnlyList<string> NeedsRequest { get; }

        public IReadOnlyDictionary<string, string> DataRepos { get; }

        public IReadOnlyDictionary<string, string> Requests { get; }

        public string DropFolder { get; }
    }
}
