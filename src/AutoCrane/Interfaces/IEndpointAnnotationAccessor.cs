// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoCrane.Interfaces
{
    public interface IEndpointAnnotationAccessor
    {
        Task<IReadOnlyDictionary<string, string>> GetEndpointAnnotationsAsync(string ns, string endpoint, CancellationToken token);

        Task PutEndpointAnnotationsAsync(string ns, string endpoint, IReadOnlyDictionary<string, string> annotationsToUpdate, CancellationToken token);
    }
}
