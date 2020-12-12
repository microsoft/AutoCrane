// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;

namespace AutoCrane.Models
{
    public sealed class DataDownloadRequest
    {
        public DataDownloadRequest(PodIdentifier pod, string repoName, string dropFolder, string extractionLocation, DataDownloadRequestDetails? details)
        {
            this.Pod = pod;
            this.DataSource = repoName;
            this.DataDropFolder = dropFolder;
            this.ExtractionLocation = extractionLocation;
            this.Details = details;
        }

        public PodIdentifier Pod { get; }

        /// <summary>
        /// The data repo this points to.
        /// </summary>
        public string DataSource { get; }

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
