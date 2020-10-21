// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using k8s;

namespace AutoCrane.Interfaces
{
    internal interface IKubernetesConfigProvider
    {
        KubernetesClientConfiguration Get();
    }
}
