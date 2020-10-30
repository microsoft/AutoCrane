// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Services
{
    internal sealed class DataRepositoryManifestWriter : IDataRepositoryManifestWriter
    {
        private readonly ILogger<DataRepositoryManifestWriter> logger;

        public DataRepositoryManifestWriter(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<DataRepositoryManifestWriter>();
        }

        public void Write(string rootDirectory, IReadOnlyList<DataRepositorySource> sources)
        {
            this.logger.LogInformation($"Writing manifest for {rootDirectory}");
        }
    }
}
