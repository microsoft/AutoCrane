// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Options;

namespace AutoCrane.Apps
{
    internal sealed class PostWatchdogService : IAutoCraneService
    {
        private readonly IWatchdogStatusPutter watchdogStatusPutter;
        private readonly IOptions<WatchdogStatus> status;
        private readonly IOptions<PodIdentifierOptions> pod;

        public PostWatchdogService(IWatchdogStatusPutter watchdogStatusPutter, IOptions<WatchdogStatus> watchdogStatus, IOptions<PodIdentifierOptions> podIdentifier)
        {
            this.watchdogStatusPutter = watchdogStatusPutter;
            this.status = watchdogStatus;
            this.pod = podIdentifier;
        }

        public async Task<int> RunAsync(CancellationToken token)
        {
            var status = this.status.Value;
            var pod = this.pod.Value;
            ThrowIfNullOrEmpty(pod?.Namespace, "Pod.Namespace");
            ThrowIfNullOrEmpty(pod?.Name, "Pod.Name");
            ThrowIfNullOrEmpty(status?.Name, "Watchdog.Name");
            ThrowIfNullOrEmpty(status?.Level, "Watchdog.Level");
            ThrowIfNullOrEmpty(status?.Message, "Watchdog.Message");

            Console.Error.WriteLine($"Set watchdog '{status.Name}' on '{pod.Namespace}'/'{pod.Name}' to '{status.Level}', message: '{status.Message}'");
            await this.watchdogStatusPutter.PutStatusAsync(pod.Identifier, status);

            return 0;
        }

        private static void ThrowIfNullOrEmpty([NotNull]string? val, string name)
        {
            if (string.IsNullOrEmpty(val))
            {
                throw new ArgumentNullException(name);
            }
        }
    }
}
