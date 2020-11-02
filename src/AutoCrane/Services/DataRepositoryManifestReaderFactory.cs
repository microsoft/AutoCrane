// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;

namespace AutoCrane.Services
{
    internal sealed class DataRepositoryManifestReaderFactory : IDataRepositoryManifestReaderFactory
    {
        public IDataRepositoryManifestReader FromStream(Stream s)
        {
            return new DropManifestReader(s);
        }

        private class DropManifestReader : IDataRepositoryManifestReader
        {
            private readonly Stream stream;

            public DropManifestReader(Stream s)
            {
                this.stream = s;
            }

            public void Dispose()
            {
            }

            public async Task<DataRepositoryManifest> ReadAsync(CancellationToken token)
            {
                var result = await JsonSerializer.DeserializeAsync<DataRepositoryManifest>(this.stream, cancellationToken: token);
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
        }
    }
}
