// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace AutoCrane.Interfaces
{
    public interface IProcessRunner
    {
        Task<IProcessResult> RunAsync(string exe, string workingDir, string[] args, CancellationToken cancellationToken);
    }
}
