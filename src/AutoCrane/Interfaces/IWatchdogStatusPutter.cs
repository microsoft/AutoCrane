// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using AutoCrane.Models;

namespace AutoCrane.Interfaces
{
    public interface IWatchdogStatusPutter
    {
        Task PutStatusAsync(PodIdentifier podIdentifier, IReadOnlyList<WatchdogStatus> status);

        Task PutStatusAsync(PodIdentifier podIdentifier, WatchdogStatus status);
    }
}
