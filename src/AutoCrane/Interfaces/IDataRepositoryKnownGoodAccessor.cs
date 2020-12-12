// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Models;

namespace AutoCrane.Interfaces
{
    public interface IDataRepositoryKnownGoodAccessor
    {
        Task<DataRepositoryKnownGoods> GetOrUpdateAsync(string ns, DataRepositoryManifest manifest, IReadOnlyList<PodDataRequestInfo> requests, CancellationToken token);
    }
}
