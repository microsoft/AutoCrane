// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace AutoCrane.Models
{
    public struct SecretCredential
    {
        public SecretCredential(string secret, DateTimeOffset expiry)
        {
            this.Secret = secret;
            this.Expiry = expiry;
        }

        public static SecretCredential Empty { get; } = new SecretCredential(string.Empty, DateTimeOffset.MinValue);

        public string Secret { get; }

        public DateTimeOffset Expiry { get; }

        public static bool operator ==(SecretCredential left, SecretCredential right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SecretCredential left, SecretCredential right)
        {
            return !(left == right);
        }

        public override bool Equals(object? other)
        {
            if (other == null)
            {
                return false;
            }

            if (other is SecretCredential pi && pi.Secret == this.Secret && pi.Expiry == this.Expiry)
            {
                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Secret.GetHashCode(), this.Expiry.GetHashCode());
        }

        public override string ToString()
        {
            return $"Secret value expiring {this.Expiry}";
        }
    }
}
