// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace AutoCrane.Models
{
    public class PodWithAnnotations
    {
        public PodWithAnnotations(PodIdentifier id, IReadOnlyDictionary<string, string> annotations)
        {
            this.Id = id;
            this.Annotations = annotations;
        }

        public PodIdentifier Id { get; }

        public IReadOnlyDictionary<string, string> Annotations { get; }
    }
}
