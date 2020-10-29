// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutoCrane.Interfaces
{
    public interface IDataRepositorySyncer
    {
        Task SyncRepoAsync(string name, string repoString, CancellationToken cancellationToken);
    }
}
