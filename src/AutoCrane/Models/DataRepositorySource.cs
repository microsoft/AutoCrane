// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace AutoCrane.Models
{
    public sealed class DataRepositorySource
    {
        public DataRepositorySource(string archiveFilePath, DateTimeOffset timestamp)
        {
            this.ArchiveFilePath = archiveFilePath;
            this.Timestamp = timestamp;
        }

        public string ArchiveFilePath { get; set; }

        public DateTimeOffset Timestamp { get; set; }
    }
}
