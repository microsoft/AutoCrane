// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace AutoCrane.Models
{
    public class PodInfo
    {
        public PodInfo(PodIdentifier id, IReadOnlyDictionary<string, string> annotations, IReadOnlyDictionary<string, bool> ready, string ipAddress)
        {
            this.Id = id;
            this.Annotations = annotations;
            this.ContainersReady = ready;
            this.PodIp = ipAddress ?? string.Empty;
        }

        public PodIdentifier Id { get; }

        public IReadOnlyDictionary<string, string> Annotations { get; }

        public IReadOnlyDictionary<string, bool> ContainersReady { get; }

        public string PodIp { get; }
    }
}
