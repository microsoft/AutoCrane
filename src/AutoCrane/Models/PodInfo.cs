// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace AutoCrane.Models
{
    public class PodInfo
    {
        public PodInfo(PodIdentifier id, IReadOnlyDictionary<string, string> annotations, string ipAddress)
        {
            this.Id = id;
            this.Annotations = annotations;
            this.PodIp = ipAddress ?? string.Empty;
        }

        public PodIdentifier Id { get; }

        public IReadOnlyDictionary<string, string> Annotations { get; }

        public string PodIp { get; }
    }
}
