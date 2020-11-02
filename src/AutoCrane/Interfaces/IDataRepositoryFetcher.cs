// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Models;

namespace AutoCrane.Interfaces
{
    public interface IDataRepositoryFetcher
    {
        bool CanFetch(string protocol);

        Task<IReadOnlyList<DataRepositorySource>> FetchAsync(string url, string scratchDir, string archiveDir, CancellationToken token);
    }
}
