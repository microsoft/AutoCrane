// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Options;

namespace AutoCrane.Apps
{
    internal sealed class GetWatchdogService
    {
        private readonly IWatchdogStatusGetter watchdogStatusGetter;
        private readonly IOptions<PodIdentifierOptions> pod;

        public GetWatchdogService(IWatchdogStatusGetter watchdogStatusGetter, IOptions<PodIdentifierOptions> podIdentifier)
        {
            this.watchdogStatusGetter = watchdogStatusGetter;
            this.pod = podIdentifier;
        }

        public async Task<int> RunAsync()
        {
            var pod = this.pod.Value;
            ThrowIfNullOrEmpty(pod?.Namespace, "Pod.Namespace");
            ThrowIfNullOrEmpty(pod?.Name, "Pod.Name");
            var result = await this.watchdogStatusGetter.GetStatusAsync(pod.Identifier);
            foreach (var item in result)
            {
                Console.WriteLine($"{item.Name},{item.Level},{item.Message}");
            }

            return 0;
        }

        private static void ThrowIfNullOrEmpty([NotNull] string? val, string name)
        {
            if (string.IsNullOrEmpty(val))
            {
                throw new ArgumentNullException(name);
            }
        }
    }
}
