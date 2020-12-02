// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Services
{
    internal sealed class DataRepositoryGitFetcher : IDataRepositoryFetcher
    {
        private const string GitExe = "/usr/bin/git";
        private const string ZstdExe = "/usr/bin/zstd";
        private const string GitCloneDepthString = "3";
        private const string GitLogDepthString = "2"; // needs to be less than clone depth
        private const string ProtocolGit = "git";
        private const string ProtocolAdoGit = "adogit";
        private readonly ILogger<DataRepositoryGitFetcher> logger;
        private readonly IProcessRunner runner;
        private readonly IFileHasher fileHasher;
        private readonly ICredentialHelper credentialHelper;

        public DataRepositoryGitFetcher(ILoggerFactory loggerFactory, IProcessRunner runner, IFileHasher fileHasher, ICredentialHelper credentialHelper)
        {
            this.logger = loggerFactory.CreateLogger<DataRepositoryGitFetcher>();
            this.runner = runner;
            this.fileHasher = fileHasher;
            this.credentialHelper = credentialHelper;
        }

        public bool CanFetch(string protocol)
        {
            return protocol == ProtocolGit || protocol == ProtocolAdoGit;
        }

        public async Task<IReadOnlyList<DataRepositorySource>> FetchAsync(string url, string scratchDir, string archiveDropDir, CancellationToken token)
        {
            this.logger.LogInformation($"FetchAsync {url} {scratchDir} {archiveDropDir}");
            var protocolAndUrl = url.Split('@', 2);
            if (protocolAndUrl.Length != 2)
            {
                throw new ArgumentOutOfRangeException(nameof(url));
            }

            var protocol = protocolAndUrl[0];
            url = protocolAndUrl[1];
            string? creds = null;
            switch (protocol)
            {
                case ProtocolGit:
                    break;
                case ProtocolAdoGit:
                    var credsAndUrl = url.Split('@', 2);
                    if (credsAndUrl.Length != 2)
                    {
                        throw new ArgumentOutOfRangeException(nameof(url));
                    }

                    var credSpec = credsAndUrl[0];
                    url = credsAndUrl[1];
                    var rawCreds = await this.credentialHelper.LookupAsync(credSpec);
                    creds = Convert.ToBase64String(Encoding.UTF8.GetBytes($":{rawCreds}"));
                    break;
                default:
                    throw new NotImplementedException($"protocol: {protocol}");
            }

            if (Directory.Exists(Path.Combine(scratchDir, ".git")))
            {
                await this.GitSyncAsync(url, scratchDir, creds, token);
            }
            else
            {
                await this.GitCloneAsync(url, scratchDir, creds, token);
            }

            if (token.IsCancellationRequested)
            {
                // do not leave in a bad state
                Directory.Delete(scratchDir);
            }

            var log = await this.GitLogAsync(scratchDir, token);
            var list = new List<DataRepositorySource>();
            foreach (var entry in log)
            {
                var archivePath = Path.Combine(archiveDropDir, $"{entry.UnixTime:x}-{entry.Hash}.tar");
                if (!File.Exists(archivePath))
                {
                    await this.GitArchiveAsync(entry.Hash, scratchDir, archivePath, token);
                }

                list.Add(new DataRepositorySource(
                    archivePath.Replace(archiveDropDir + Path.DirectorySeparatorChar, string.Empty),
                    await this.fileHasher.GetAsync(archivePath, cacheOnDisk: true),
                    DateTimeOffset.FromUnixTimeSeconds(entry.UnixTime)));
            }

            return list;
        }

        private async Task GitArchiveAsync(string hash, string scratchDir, string archivePath, CancellationToken token)
        {
            var tmpFile = archivePath + ".tmp";

            try
            {
                var result = await this.runner.RunAsync(GitExe, scratchDir, new string[] { "archive", "--format=tar", hash, "-o", tmpFile }, token);
                result.ThrowIfFailed();
                token.ThrowIfCancellationRequested();

                result = await this.runner.RunAsync(ZstdExe, scratchDir, new string[] { tmpFile, "-o", archivePath }, token);
                result.ThrowIfFailed();
            }
            catch (Exception)
            {
                if (File.Exists(archivePath))
                {
                    File.Delete(archivePath);
                }

                throw;
            }
            finally
            {
                if (File.Exists(tmpFile))
                {
                    File.Delete(tmpFile);
                }
            }
        }

        private async Task<List<GitLogEntry>> GitLogAsync(string scratchDir, CancellationToken token)
        {
            var logEntries = new List<GitLogEntry>();

            var result = await this.runner.RunAsync(GitExe, scratchDir, new string[] { "log", "--format=%H %ct", $"-{GitLogDepthString}" }, token);
            result.ThrowIfFailed();

            foreach (var line in result.OutputText)
            {
                var splits = line.Trim().Split(' ');
                if (splits.Length == 2)
                {
                    var time = long.Parse(splits[1]);
                    logEntries.Add(new GitLogEntry(splits[0], time));
                }
            }

            return logEntries;
        }

        private async Task GitCloneAsync(string url, string dir, string? creds, CancellationToken token)
        {
            IProcessResult? result;
            if (creds == null)
            {
                result = await this.runner.RunAsync(GitExe, dir, new string[] { "clone", url, ".", "--depth", GitCloneDepthString }, token);
            }
            else
            {
                result = await this.runner.RunAsync(GitExe, dir, new string[] { "-c", $"http.extraheader=authorization: basic {creds}", "clone", url, ".", "--depth", GitCloneDepthString }, token, new string[] { creds });
            }

            result.ThrowIfFailed();
        }

        private async Task GitSyncAsync(string url, string dir, string? creds, CancellationToken token)
        {
            // look at setting remote to url before git pull?
            if (url is null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            try
            {
                if (creds == null)
                {
                    var result = await this.runner.RunAsync(GitExe, dir, new string[] { "fetch", "--depth", GitCloneDepthString, "origin" }, token);
                    result.ThrowIfFailed();

                    result = await this.runner.RunAsync(GitExe, dir, new string[] { "checkout", "FETCH_HEAD" }, token);
                    result.ThrowIfFailed();
                }
                else
                {
                    var result = await this.runner.RunAsync(GitExe, dir, new string[] { "-c", $"http.extraheader=authorization: basic {creds}", "fetch", "--depth", GitCloneDepthString, "origin" }, token, new string[] { creds });
                    result.ThrowIfFailed();

                    result = await this.runner.RunAsync(GitExe, dir, new string[] { "-c", $"http.extraheader=authorization: basic {creds}", "checkout", "FETCH_HEAD" }, token, new string[] { creds });
                    result.ThrowIfFailed();
                }
            }
            finally
            {
                var lockFile = Path.Combine(dir, ".git", "shallow.lock");
                if (File.Exists(lockFile))
                {
                    this.logger.LogInformation($"Deleting lock file: {lockFile}");
                    File.Delete(lockFile);
                }
            }
        }

        private class GitLogEntry
        {
            public GitLogEntry(string hash, long unixTime)
            {
                this.Hash = hash;
                this.UnixTime = unixTime;
            }

            public string Hash { get; set; }

            public long UnixTime { get; set; }
        }
    }
}
