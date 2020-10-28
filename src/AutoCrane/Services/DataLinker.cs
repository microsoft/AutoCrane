// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Services
{
    internal sealed class DataLinker : IDataLinker
    {
        private readonly ILogger<DataLinker> logger;

        public DataLinker(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<DataLinker>();
        }

        public Task LinkAsync(string fromPath, string toPath)
        {
            using (var process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "/bin/ln",
                },
            })
            {
                process.StartInfo.ArgumentList.Add("-s");
                process.StartInfo.ArgumentList.Add(fromPath);
                process.StartInfo.ArgumentList.Add(toPath);
                process.Start();
                this.logger.LogInformation($"Running: ln -fs {fromPath} {toPath}");
                return process.WaitForExitAsync();
            }
        }
    }
}
