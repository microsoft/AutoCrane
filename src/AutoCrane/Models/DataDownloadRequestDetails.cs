// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;

namespace AutoCrane.Models
{
    public sealed class DataDownloadRequestDetails
    {
        /// <summary>
        /// The data repository host name.
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// A hash of the contents of the archive.
        /// </summary>
        public string? Hash { get; set; }

        /// <summary>
        /// Number of seconds since the unix epoch.
        /// </summary>
        public long? UnixTimestampSeconds { get; set; }

    }
}
