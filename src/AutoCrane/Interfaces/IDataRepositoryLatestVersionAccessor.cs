// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Models;

namespace AutoCrane.Interfaces
{
    public interface IDataRepositoryLatestVersionAccessor
    {
        Task<DataRepositoryLatestVersionInfo> GetOrUpdateAsync(string ns, DataRepositoryManifest manifest, CancellationToken token);
    }
}
