// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Options;

namespace AutoCrane.Services
{
    public sealed class AutoCraneConfig : IAutoCraneConfig
    {
        private readonly HashSet<string> allowedNamespaces;

        public AutoCraneConfig(IOptions<AutoCraneOptions> options)
        {
            this.allowedNamespaces = new HashSet<string>();
            if (options.Value.EvictionDeleteGracePeriodSeconds.HasValue)
            {
                this.EvictionDeleteGracePeriodSeconds = options.Value.EvictionDeleteGracePeriodSeconds.Value;
            }
            else
            {
                this.EvictionDeleteGracePeriodSeconds = 120;
            }

            if (options.Value.WatchdogProbeTimeoutSeconds.HasValue)
            {
                this.WatchdogProbeTimeout = TimeSpan.FromSeconds(options.Value.WatchdogProbeTimeoutSeconds.Value);
            }
            else
            {
                this.WatchdogProbeTimeout = TimeSpan.FromSeconds(5);
            }

            if (string.IsNullOrEmpty(options.Value.Namespaces))
            {
                return;
            }

            foreach (var item in options.Value.Namespaces.Split(',').Select(s => s.Trim()))
            {
                this.allowedNamespaces.Add(item);
            }
        }

        public IEnumerable<string> Namespaces => this.allowedNamespaces;

        public long EvictionDeleteGracePeriodSeconds { get; private set; }

        public TimeSpan WatchdogProbeTimeout { get; private set; }

        public bool IsAllowedNamespace(string ns)
        {
            return this.allowedNamespaces.Contains(ns);
        }
    }
}
