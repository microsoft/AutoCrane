// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using AutoCrane.Interfaces;

namespace AutoCrane.Services
{
    internal sealed class DataDownloader : IDataDownloader
    {
        public DataDownloader()
        {
        }

        public Task DownloadAsync(string name, string method, string sourceRef, string storeUrl, string storeLocation)
        {
            throw new NotImplementedException();
        }
    }
}
