// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using k8s;
using Microsoft.Extensions.Options;

namespace AutoCrane.Services
{
    internal sealed class KubernetesConfigProvider : IKubernetesConfigProvider
    {
        private readonly IOptions<KubernetesConfig> options;

        public KubernetesConfigProvider(IOptions<KubernetesConfig> options)
        {
            this.options = options;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1122:Use string.Empty for empty strings", Justification = "cannot")]
        public KubernetesClientConfiguration Get()
        {
            var location = this.options.Value.Location ?? string.Empty;
            return location switch
            {
                "default" or "" => KubernetesClientConfiguration.BuildDefaultConfig(),
                "kubeconfig" => KubernetesClientConfiguration.BuildConfigFromConfigFile(Environment.GetEnvironmentVariable("KUBECONFIG")),
                "cluster" => KubernetesClientConfiguration.InClusterConfig(),
                _ => throw new ArgumentOutOfRangeException("Kubeconfig.Location"),
            };
        }
    }
}
