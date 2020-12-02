// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading.Tasks;

namespace AutoCrane.Interfaces
{
    public interface IFileHasher
    {
        Task<string> CalculateAsync(string filename, bool cacheOnDisk = false);
    }
}
