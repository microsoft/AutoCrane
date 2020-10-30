﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly ILogger<DataRepositoryGitFetcher> logger;
        private readonly IProcessRunner runner;

        public DataRepositoryGitFetcher(ILoggerFactory loggerFactory, IProcessRunner runner)
        {
            this.logger = loggerFactory.CreateLogger<DataRepositoryGitFetcher>();
            this.runner = runner;
        }

        public bool CanFetch(string protocol)
        {
            return protocol == "git";
        }

        public async Task<IReadOnlyList<DataRepositorySource>> FetchAsync(string url, string scratchDir, string archiveDropDir, CancellationToken token)
        {
            if (Directory.Exists(Path.Combine(scratchDir, ".git")))
            {
                await this.GitSyncAsync(url, scratchDir, token);
            }
            else
            {
                await this.GitCloneAsync(url, scratchDir, token);
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

                list.Add(new DataRepositorySource(archivePath, DateTimeOffset.FromUnixTimeMilliseconds(entry.UnixTime)));
            }

            return list;
        }

        private async Task GitArchiveAsync(string hash, string scratchDir, string archivePath, CancellationToken token)
        {
            var result = await this.runner.RunAsync(GitExe, scratchDir, new string[] { "archive", "--format=tar", hash, "-o", archivePath }, token);
            result.ThrowIfFailed();
            result = await this.runner.RunAsync(ZstdExe, scratchDir, new string[] { "-19", archivePath }, token);
            result.ThrowIfFailed();
        }

        private async Task<List<GitLogEntry>> GitLogAsync(string scratchDir, CancellationToken token)
        {
            var logEntries = new List<GitLogEntry>();

            var result = await this.runner.RunAsync(GitExe, scratchDir, new string[] { "log", "--format=%H %ct", "HEAD~5..HEAD" }, token);
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

        private async Task GitCloneAsync(string url, string dir, CancellationToken token)
        {
            var result = await this.runner.RunAsync(GitExe, dir, new string[] { "clone", url, "." }, token);
            result.ThrowIfFailed();
        }

        private async Task GitSyncAsync(string url, string dir, CancellationToken token)
        {
            // look at setting remote to url before git pull?
            if (url is null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            var result = await this.runner.RunAsync(GitExe, dir, new string[] { "pull", "origin" }, token);
            result.ThrowIfFailed();
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
