// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutoCrane.Interfaces
{
    public interface IExpiredObjectDeleter
    {
        Task DeleteExpiredObjectsAsync(string ns, DateTimeOffset now, CancellationToken token);
    }
}
