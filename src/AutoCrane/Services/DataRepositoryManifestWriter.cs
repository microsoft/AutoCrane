// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoCrane.Services
{
    internal sealed class DataRepositoryManifestWriter : IDataRepositoryManifestWriter
    {
        private readonly ILogger<DataRepositoryManifestWriter> logger;

        public DataRepositoryManifestWriter(ILoggerFactory loggerFactory, IOptions<DataRepoOptions> options)
        {
            this.logger = loggerFactory.CreateLogger<DataRepositoryManifestWriter>();
            var rootDirectory = options.Value.ArchivePath;
            if (rootDirectory is null)
            {
                throw new ArgumentNullException($"DataRepoOptions:ArchivePath is null");
            }

            Directory.CreateDirectory(rootDirectory);
            this.ManifestFilePath = Path.Combine(rootDirectory, "manifest.txt");
        }

        public string ManifestFilePath { get; private set; }

        public void Write(IReadOnlyDictionary<string, IReadOnlyList<DataRepositorySource>> sources)
        {
            this.logger.LogInformation($"Writing manifest for {this.ManifestFilePath}");

        }
    }
}
