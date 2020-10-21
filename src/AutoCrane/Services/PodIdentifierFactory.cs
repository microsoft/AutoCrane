// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using AutoCrane.Interfaces;
using AutoCrane.Models;

namespace AutoCrane.Services
{
    internal sealed class PodIdentifierFactory : IPodIdentifierFactory
    {
        public PodIdentifier FromString(string id)
        {
            var splits = id.Split('/', 2);
            if (splits.Length == 1)
            {
                return new PodIdentifier("default", splits[0]);
            }

            return new PodIdentifier(splits[0], splits[1]);
        }

        public PodIdentifier FromString(string ns, string id)
        {
            return new PodIdentifier(ns, id);
        }
    }
}
