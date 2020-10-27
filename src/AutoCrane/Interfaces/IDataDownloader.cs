// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoCrane.Interfaces
{
    public interface IDataDownloader
    {
        Task DownloadAsync(string name, string method, string sourceRef, string storeUrl, string storeLocation);
    }
}
