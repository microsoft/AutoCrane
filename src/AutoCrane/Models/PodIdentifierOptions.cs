// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace AutoCrane.Models
{
    public sealed class PodIdentifierOptions
    {
        public string? Name { get; set; }

        public string? Namespace { get; set; }

        public PodIdentifier Identifier => new PodIdentifier(this.Namespace ?? string.Empty, this.Name ?? string.Empty);
    }
}
