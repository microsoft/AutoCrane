// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace AutoCrane.Interfaces
{
    public interface IExpiredObjectDeleter
    {
        Task DeleteAsync(string ns, CancellationToken token);
    }
}
