// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;

namespace AutoCrane.Services
{
    internal sealed class DataLinker : IDataLinker
    {
        private readonly IProcessRunner runner;

        public DataLinker(IProcessRunner runner)
        {
            this.runner = runner;
        }

        public async Task LinkAsync(string fromPath, string toPath, CancellationToken token)
        {
            var result = await this.runner.RunAsync("/bin/ln", null, new string[] { "-sf", fromPath, toPath }, token);
            result.ThrowIfFailed();
        }
    }
}
