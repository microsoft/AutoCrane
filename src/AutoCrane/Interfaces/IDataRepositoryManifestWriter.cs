﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using AutoCrane.Models;

namespace AutoCrane.Interfaces
{
    public interface IDataRepositoryManifestWriter
    {
        string ManifestFilePath { get; }

        void Write(IReadOnlyDictionary<string, IReadOnlyList<DataRepositorySource>> sources);
    }
}
