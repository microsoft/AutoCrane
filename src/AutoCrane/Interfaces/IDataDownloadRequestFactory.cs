// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using AutoCrane.Models;

namespace AutoCrane.Interfaces
{
    public interface IDataDownloadRequestFactory
    {
        Task<IList<DataDownloadRequest>> GetPodRequestsAsync();

        Task<IList<DataDownloadRequest>> GetPodRequestsAsync(PodIdentifier pod);
    }
}
