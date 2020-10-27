// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace AutoCrane.Models
{
    public sealed class DropManifestEntry
    {
        public DropManifestEntry(string refName, string fileHash, string mapTo)
        {
            this.RefName = refName;
            this.FileHash = fileHash;
            this.MapTo = mapTo;
        }

        public string RefName { get; }

        public string FileHash { get; }

        public string MapTo { get; }
    }
}
