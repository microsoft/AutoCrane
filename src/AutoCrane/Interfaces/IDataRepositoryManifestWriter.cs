// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading.Tasks;
using AutoCrane.Models;

namespace AutoCrane.Interfaces
{
    public interface IDataRepositoryManifestWriter
    {
        string ManifestFilePath { get; }

        Task WriteAsync(DataRepositoryManifest manifest);
    }
}
