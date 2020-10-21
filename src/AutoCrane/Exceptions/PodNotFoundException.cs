// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using AutoCrane.Models;

namespace AutoCrane.Exceptions
{
    public class PodNotFoundException : Exception
    {
        public PodNotFoundException(PodIdentifier pod)
            : base(pod.Namespace + '/' + pod.Name)
        {
        }
    }
}
