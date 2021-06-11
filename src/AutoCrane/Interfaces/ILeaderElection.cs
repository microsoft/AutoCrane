// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutoCrane.Interfaces
{
    public interface ILeaderElection
    {
        bool IsLeader { get; }

        Task StartBackgroundTask(string objectName, TimeSpan leaseDuration, CancellationToken token);
    }
}
