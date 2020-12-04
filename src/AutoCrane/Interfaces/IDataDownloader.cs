// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Models;

namespace AutoCrane.Interfaces
{
    public interface IDataDownloader
    {
        string GetDropDownloadArchiveName(string dropFolder, string hash);

        Task DownloadAsync(DataDownloadRequest request, CancellationToken token);
    }
}
