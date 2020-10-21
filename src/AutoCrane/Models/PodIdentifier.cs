// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace AutoCrane.Models
{
    public struct PodIdentifier
    {
        public PodIdentifier(string ns, string name)
        {
            this.Name = name;
            this.Namespace = ns;
        }

        public string Name { get; }

        public string Namespace { get; }

        public static bool operator ==(PodIdentifier left, PodIdentifier right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PodIdentifier left, PodIdentifier right)
        {
            return !(left == right);
        }

        public override bool Equals(object? other)
        {
            if (other == null)
            {
                return false;
            }

            if (other is PodIdentifier pi && pi.Name == this.Name && pi.Namespace == this.Namespace)
            {
                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Name.GetHashCode(), this.Namespace.GetHashCode());
        }
    }
}
