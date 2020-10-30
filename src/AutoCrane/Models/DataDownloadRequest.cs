// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;

namespace AutoCrane.Models
{
    public sealed class DataDownloadRequest
    {
        public DataDownloadRequest(PodIdentifier pod, string name, string repoHostname, string dropFolder, string repoFilename, string hashToMatch, string extractionLocation)
        {
            this.Pod = pod;
            this.Name = name;
            this.DataRepositoryHostname = repoHostname;
            this.DataRepositoryFilename = repoFilename;
            this.DataDropFolder = dropFolder;
            this.HashToMatch = hashToMatch;
            this.ExtractionLocation = extractionLocation;

            if (this.HashToMatch.Any(ch => char.IsLetterOrDigit(ch)))
            {
                throw new ArgumentOutOfRangeException(nameof(this.HashToMatch));
            }
        }

        public PodIdentifier Pod { get; }

        /// <summary>
        /// The name of this data deployment.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The data repository host name.
        /// </summary>
        public string DataRepositoryHostname { get; }

        /// <summary>
        /// The file name on data repository host with the data.
        /// </summary>
        public string DataRepositoryFilename { get; }

        /// <summary>
        /// The folder to put the extracted data into.
        /// </summary>
        public string DataDropFolder { get; }

        /// <summary>
        /// A hash of the contents of the archive.
        /// </summary>
        public string HashToMatch { get; }

        /// <summary>
        /// The place to put the extracted archive
        /// </summary>
        public string ExtractionLocation { get; }
    }
}
