// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Services
{
    internal sealed class ProcessRunner : IProcessRunner
    {
        private const string SecretMaskString = "***secret***";
        private readonly ILogger<ProcessRunner> logger;

        public ProcessRunner(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<ProcessRunner>();
        }

        public Task<IProcessResult> RunAsync(string exe, string? workingDir, string[] args, CancellationToken cancellationToken)
        {
            return this.RunAsync(exe, workingDir, args, cancellationToken, Array.Empty<string>());
        }

        public async Task<IProcessResult> RunAsync(string exe, string? workingDir, string[] args, CancellationToken cancellationToken, string[] secretsToMask)
        {
            var result = new ProcessResult()
            {
                Executable = exe,
            };

            using var process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = exe,
                    WorkingDirectory = workingDir ?? string.Empty,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
            };

            foreach (var arg in args)
            {
                process.StartInfo.ArgumentList.Add(arg);
            }

            process.OutputDataReceived += (sender, eventArgs) =>
            {
                var line = eventArgs.Data;
                if (line != null)
                {
                    this.logger.LogInformation($"{exe}: {line}");
                    result.OutputText.Add(line);
                }
            };

            process.ErrorDataReceived += (sender, eventArgs) =>
            {
                var line = eventArgs.Data;
                if (line != null)
                {
                    this.logger.LogError($"{exe}: {line}");
                    result.ErrorText.Add(line);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            this.logger.LogInformation($"Running: {exe} {string.Join(' ', args.Select(a => $"\"{MaskSecrets(a, secretsToMask)}\""))}");
            await process.WaitForExitAsync(cancellationToken);

            result.ExitCode = process.ExitCode;
            return result;
        }

        private static string MaskSecrets(string str, string[] secretsToMask)
        {
            foreach (var secret in secretsToMask)
            {
                str = str.Replace(secret, SecretMaskString);
            }

            return str;
        }

        private class ProcessResult : IProcessResult
        {
            public string Executable { get; set; } = string.Empty;

            public int ExitCode { get; set; }

            public IList<string> OutputText { get; set; } = new List<string>();

            public IList<string> ErrorText { get; set; } = new List<string>();

            public void ThrowIfFailed()
            {
                if (this.ExitCode != 0)
                {
                    throw new InvalidOperationException($"{this.Executable} failed with exit code {this.ExitCode}");
                }
            }
        }
    }
}
