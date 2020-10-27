// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace AutoCrane.Models
{
    public sealed class DataDownloadResult
    {
        public DataDownloadResult(PodIdentifier pod, string name, string sourceRef)
        {
            this.Pod = pod;
            this.Name = name;
            this.SourceRef = sourceRef;
        }

        public PodIdentifier Pod { get; }

        public string Name { get; }

        public string SourceRef { get; }
    }
}
