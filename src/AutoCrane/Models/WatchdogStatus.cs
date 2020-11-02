// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace AutoCrane.Models
{
    public sealed class WatchdogStatus
    {
        internal const string Prefix = "status.autocrane.io/";
        internal const string ErrorLevel = "error";
        internal const string InfoLevel = "info";

        public static ISet<string> ValidLevels { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "error", "warning", "info" };

        public string? Name { get; set; }

        public string? Level { get; set; }

        public string? Message { get; set; }

        public bool IsFailure => ErrorLevel.Equals(this.Level ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
