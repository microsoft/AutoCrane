// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Services
{
    internal sealed class DataRepositoryManifestReaderFactory : IDataRepositoryManifestReaderFactory
    {
        private readonly ILoggerFactory loggerFactory;

        public DataRepositoryManifestReaderFactory(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
        }

        public IDataRepositoryManifestReader FromStream(Stream s)
        {
            return new DropManifestReader(s, this.loggerFactory.CreateLogger<DropManifestReader>());
        }

        private class DropManifestReader : IDataRepositoryManifestReader
        {
            private readonly Stream stream;
            private readonly ILogger<DropManifestReader> logger;

            public DropManifestReader(Stream s, ILogger<DropManifestReader> logger)
            {
                this.stream = s;
                this.logger = logger;
            }

            public void Dispose()
            {
            }

            public async Task<DataRepositoryManifest> ReadAsync(CancellationToken token)
            {
                using var ms = new MemoryStream();
                await this.stream.CopyToAsync(ms, cancellationToken: token);
                var array = ms.ToArray();
                try
                {
                    var result = JsonSerializer.Deserialize<DataRepositoryManifest>(array);
                    if (result is null)
                    {
                        throw new InvalidDataException(nameof(DataRepositoryManifest));
                    }

                    if (result.Sources is null)
                    {
                        throw new InvalidDataException(nameof(DataRepositoryManifest));
                    }

                    return result;
                }
                catch (Exception)
                {
                    this.logger.LogError($"Error deserializing: " + Encoding.UTF8.GetString(array));
                    throw;
                }
            }
        }
    }
}
