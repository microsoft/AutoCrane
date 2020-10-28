﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading.Tasks;
using AutoCrane.Models;

namespace AutoCrane.Interfaces
{
    public interface IDataLinker
    {
        Task LinkAsync(string fromPath, string toPath);
    }
}
