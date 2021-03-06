﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.IO;

namespace AutoCrane.Interfaces
{
    public interface IDataRepositoryManifestReaderFactory
    {
        IDataRepositoryManifestReader FromStream(Stream s);
    }
}
