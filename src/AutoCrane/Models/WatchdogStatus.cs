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

        public static ISet<string> ValidLevels { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "error", "warning", "info" };

        public string? Name { get; set; }

        public string? Level { get; set; }

        public string? Message { get; set; }
    }
}
