// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;

namespace AutoCrane.Models
{
    public sealed class DataDownloadRequest
    {
        public DataDownloadRequest(PodIdentifier pod, string localName, string repoName, string dropFolder, string extractionLocation, DataDownloadRequestDetails? details)
        {
            this.Pod = pod;
            this.LocalName = localName;
            this.RepoName = repoName;
            this.DataDropFolder = dropFolder;
            this.ExtractionLocation = extractionLocation;
            this.Details = details;
        }

        public PodIdentifier Pod { get; }

        /// <summary>
        /// The name of this data deployment for this pod.
        /// </summary>
        public string LocalName { get; }

        /// <summary>
        /// The data repo this points to.
        /// </summary>
        public string RepoName { get; }

        /// <summary>
        /// The folder to put the extracted data into.
        /// </summary>
        public string DataDropFolder { get; }

        /// <summary>
        /// The place to put the extracted archive.
        /// </summary>
        public string ExtractionLocation { get; }

        public DataDownloadRequestDetails? Details { get; }
    }
}
