// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoCrane.Services
{
    internal sealed class DataRepositoryKnownGoodAccessor : IDataRepositoryKnownGoodAccessor
    {
        private readonly ILogger<DataRepositoryKnownGoodAccessor> logger;

        public DataRepositoryKnownGoodAccessor(ILoggerFactory loggerFactory, IOptions<DataRepoOptions> options, IDataRepositoryManifestReaderFactory manifestReaderFactory)
        {
            this.logger = loggerFactory.CreateLogger<DataRepositoryKnownGoodAccessor>();
        }

        public Task<DataRepositoryKnownGoods> GetOrCreateAsync(DataRepositoryManifest manifest, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
