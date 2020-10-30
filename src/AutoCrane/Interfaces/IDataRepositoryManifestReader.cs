// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using AutoCrane.Models;

namespace AutoCrane.Interfaces
{
    public interface IDataRepositoryManifestReader : IDisposable
    {
        IEnumerable<DropManifestEntry> Read();
    }
}
