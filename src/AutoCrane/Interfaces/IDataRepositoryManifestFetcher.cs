// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Models;

namespace AutoCrane.Interfaces
{
    public interface IDataRepositoryManifestFetcher
    {
        Task<DataRepositoryManifest> FetchAsync(CancellationToken token);
    }
}
