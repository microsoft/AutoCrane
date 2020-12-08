// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace AutoCrane.Models
{
    public sealed class DataRepositoryKnownGoods
    {
        public DataRepositoryKnownGoods(IDictionary<string, string> knownGoodVersions)
        {
            this.KnownGoodVersions = knownGoodVersions;
        }

        public IDictionary<string, string> KnownGoodVersions { get; }
    }
}
