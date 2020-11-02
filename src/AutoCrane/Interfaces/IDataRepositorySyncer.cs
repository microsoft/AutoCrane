// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Models;

namespace AutoCrane.Interfaces
{
    public interface IDataRepositorySyncer
    {
        Task<IReadOnlyList<DataRepositorySource>> SyncRepoAsync(string sourcePath, string archivePath, string repoString, CancellationToken cancellationToken);
    }
}
