// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace AutoCrane.Interfaces
{
    public interface IServiceHeartbeat
    {
        void Beat(string name);

        TimeSpan? GetLastBeat(string name);
    }
}
