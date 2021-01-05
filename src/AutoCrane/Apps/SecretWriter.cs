// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoCrane.Apps
{
    public sealed class SecretWriter : IAutoCraneService
    {
        private const int ConsecutiveErrorCountBeforeExiting = 3;
        private const int IterationLoopSeconds = 15;
        private readonly ILogger<SecretWriter> logger;
        private readonly IOptions<SecretWriterOptions> options;
        private readonly ICredentialHelper credentialHelper;

        public SecretWriter(ILoggerFactory loggerFactory, IOptions<SecretWriterOptions> options, ICredentialHelper credentialHelper)
        {
            this.logger = loggerFactory.CreateLogger<SecretWriter>();
            this.options = options;
            this.credentialHelper = credentialHelper;
        }

        public async Task<int> RunAsync(CancellationToken token)
        {
            var errorCount = 0;

            while (errorCount < ConsecutiveErrorCountBeforeExiting)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    // write secrets to files
                    // format: /path/to/file:secretspec;/path/to/file2:secretspec2
                    var sources = this.options.Value.Sources?.Split(';').Select(s => s.Split(':', 2)).Where(ss => ss.Length == 2).Select(ss =>
                    {
                        return new KeyValuePair<string, string>(ss[0], ss[1]);
                    }).ToDictionary(x => x.Key, x => x.Value);

                    if (sources is null)
                    {
                        throw new ArgumentNullException(nameof(sources));
                    }

                    foreach (var entry in sources)
                    {
                        var filename = entry.Key;
                        var credSpec = entry.Value;
                        this.logger.LogInformation($"Writing secret spec '{credSpec}' to {filename}");
                        var secret = await this.credentialHelper.LookupAsync(credSpec);
                        File.WriteAllText(filename, secret.Secret);
                    }

                    this.logger.LogInformation($"Finished writing {sources.Count} secrets");
                    return 0;
                }
                catch (Exception e)
                {
                    this.logger.LogError($"Unhandled exception: {e}");
                    errorCount++;
                }

                await Task.Delay(TimeSpan.FromSeconds(IterationLoopSeconds), token);
            }

            this.logger.LogError($"Hit max consecutive error count...exiting...");
            return 1;
        }
    }
}
