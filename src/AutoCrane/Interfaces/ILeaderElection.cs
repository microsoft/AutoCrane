// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace AutoCrane.Interfaces
{
    public interface ILeaderElection
    {
        bool IsLeader { get; }
    }
}
