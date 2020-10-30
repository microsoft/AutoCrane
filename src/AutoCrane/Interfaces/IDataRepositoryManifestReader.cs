// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Models;

namespace AutoCrane.Interfaces
{
    public interface IDataRepositoryManifestReader : IDisposable
    {
        Task<DataRepositoryManifest> ReadAsync(CancellationToken cancellationToken);
    }
}
