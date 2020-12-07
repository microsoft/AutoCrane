// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace AutoCrane.Interfaces
{
    public interface IAutoCraneService
    {
        Task<int> RunAsync(CancellationToken token);
    }
}
