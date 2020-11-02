// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace AutoCrane.Models
{
    public sealed class DataRepositoryManifest
    {
        public DataRepositoryManifest(IReadOnlyDictionary<string, IReadOnlyList<DataRepositorySource>> sources)
        {
            this.Sources = sources;
        }

        public IReadOnlyDictionary<string, IReadOnlyList<DataRepositorySource>> Sources { get; internal set; }
    }
}
