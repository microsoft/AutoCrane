// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoCrane.Apps
{
    internal sealed class PostWatchdogService : IAutoCraneService
    {
        private readonly IWatchdogStatusPutter watchdogStatusPutter;
        private readonly IOptions<WatchdogStatus> status;
        private readonly IOptions<PodIdentifierOptions> pod;
        private readonly ILogger<PostWatchdogService> logger;

        public PostWatchdogService(IWatchdogStatusPutter watchdogStatusPutter, IOptions<WatchdogStatus> watchdogStatus, IOptions<PodIdentifierOptions> podIdentifier, ILogger<PostWatchdogService> logger)
        {
            this.watchdogStatusPutter = watchdogStatusPutter;
            this.status = watchdogStatus;
            this.pod = podIdentifier;
            this.logger = logger;
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

            this.logger.LogError("Set watchdog '{watchdogName}' on '{podNamespace}'/'{podName}' to '{statusLevel}', message: '{statusMessage}'", status.Name, pod.Namespace, pod.Name, status.Level.ToString(), status.Message);
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
