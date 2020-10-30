// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace AutoCrane.Interfaces
{
    public interface IDataLinker
    {
        Task LinkAsync(string fromPath, string toPath, CancellationToken token);
    }
}
