// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace AutoCrane.Models
{
    public sealed class DataRepositoryKnownGoods
    {
        public DataRepositoryKnownGoods(IReadOnlyDictionary<string, string> knownGoodVersions)
        {
            this.KnownGoodVersions = knownGoodVersions;
        }

        public IReadOnlyDictionary<string, string> KnownGoodVersions { get; }
    }
}
