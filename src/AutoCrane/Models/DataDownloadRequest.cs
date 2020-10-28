// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace AutoCrane.Models
{
    public sealed class DataDownloadRequest
    {
        public DataDownloadRequest(PodIdentifier pod, string name, string storeUrl, string storeLocation, string sourceRef, string hashToMatch)
        {
            this.Pod = pod;
            this.Name = name;
            this.StoreUrl = storeUrl;
            this.SourceRef = sourceRef;
            this.StoreLocation = storeLocation;
            this.HashToMatch = hashToMatch;
        }

        public PodIdentifier Pod { get; }

        public string Name { get; }

        public string StoreUrl { get; }

        public string SourceRef { get; }

        public string StoreLocation { get; }

        public string HashToMatch { get; }
    }
}
