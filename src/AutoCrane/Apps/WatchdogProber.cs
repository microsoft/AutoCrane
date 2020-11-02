// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using Microsoft.Extensions.Logging;

namespace AutoCrane.Apps
{
    public sealed class WatchdogProber
    {
        private const string ProbeAnnotationPrefix = "probe.autocrane.io/";
        private const string WatchdogUrlRegexString = "POD_IP:([0-9]+)/([A-Za-z0-9_-]+)";
        private const int ConsecutiveErrorCountBeforeExiting = 5;
        private const int IterationLoopSeconds = 10;
        private static readonly Regex WatchdogUrlRegex = new Regex(WatchdogUrlRegexString, RegexOptions.Compiled);
        private readonly IAutoCraneConfig config;
        private readonly IPodGetter podGetter;
        private readonly IWatchdogStatusPutter watchdogStatusPutter;
        private readonly ILogger<WatchdogProber> logger;
        private readonly HttpClient httpClient;

        public WatchdogProber(IAutoCraneConfig config, ILoggerFactory loggerFactory, IPodGetter podGetter, IWatchdogStatusPutter watchdogStatusPutter)
        {
            this.config = config;
            this.podGetter = podGetter;
            this.watchdogStatusPutter = watchdogStatusPutter;
            this.logger = loggerFactory.CreateLogger<WatchdogProber>();
            this.httpClient = new HttpClient();
        }

        public async Task<int> RunAsync(int iterations = int.MaxValue)
        {
            var errorCount = 0;
            if (!this.config.Namespaces.Any())
            {
                this.logger.LogError($"No namespaces configured to watch... set env var AutoCrane__Namespaces to a comma-separated value");
                return 3;
            }

            while (iterations > 0)
            {
                if (errorCount > ConsecutiveErrorCountBeforeExiting)
                {
                    this.logger.LogError($"Hit max consecutive error count...exiting...");
                    return 2;
                }

                try
                {
                    var podCount = 0;
                    var nsCount = 0;
                    var probeCount = 0;
                    var sw = Stopwatch.StartNew();

                    foreach (var ns in this.config.Namespaces)
                    {
                        nsCount++;
                        var pods = await this.podGetter.GetPodsAsync(ns);
                        foreach (var pod in pods)
                        {
                            podCount++;
                            var watchdogsToPut = new List<WatchdogStatus>();
                            foreach (var annotation in pod.Annotations)
                            {
                                if (annotation.Key.StartsWith(ProbeAnnotationPrefix))
                                {
                                    probeCount++;
                                    var wdName = annotation.Key.Replace(ProbeAnnotationPrefix, string.Empty);
                                    var wdAnnotationName = WatchdogStatus.Prefix + wdName;
                                    var alreadyInErrorState = pod.Annotations.Any(pa => pa.Key == wdAnnotationName && pa.Value.StartsWith(WatchdogStatus.ErrorLevel));

                                    var error = await this.ProbePod(pod.PodIp, annotation.Value);
                                    if (error != null && !alreadyInErrorState)
                                    {
                                        this.logger.LogWarning($"Watchdog {wdName} Error on {pod.Id}: {error}");

                                        watchdogsToPut.Add(new WatchdogStatus()
                                        {
                                            Name = wdName,
                                            Level = WatchdogStatus.ErrorLevel,
                                            Message = error,
                                        });
                                    }
                                    else if (error == null && alreadyInErrorState)
                                    {
                                        // we need to clear the error
                                        this.logger.LogWarning($"Watchdog {wdName} OK on {pod.Id}");

                                        watchdogsToPut.Add(new WatchdogStatus()
                                        {
                                            Name = wdName,
                                            Level = WatchdogStatus.InfoLevel,
                                            Message = $"error cleared",
                                        });
                                    }
                                }
                            }

                            if (watchdogsToPut.Any())
                            {
                                await this.watchdogStatusPutter.PutStatusAsync(pod.Id, watchdogsToPut);
                            }
                        }
                    }

                    sw.Stop();
                    this.logger.LogInformation($"Scanned {nsCount} namespaces, {podCount} pods, and {probeCount} probes in {sw.Elapsed}");
                    await Task.Delay(TimeSpan.FromSeconds(IterationLoopSeconds));
                    iterations--;
                    errorCount = 0;
                }
                catch (Exception e)
                {
                    this.logger.LogError($"Unhandled exception: {e}");
                    errorCount++;
                    await Task.Delay(TimeSpan.FromSeconds(IterationLoopSeconds));
                }
            }

            return 0;
        }

        private async Task<string?> ProbePod(string podIp, string urlTemplate)
        {
            this.logger.LogDebug($"Probing pod {podIp} at {urlTemplate}");
            var match = WatchdogUrlRegex.Match(urlTemplate);
            if (!match.Success)
            {
                return $"Url {urlTemplate} did not match regex: {WatchdogUrlRegexString}";
            }

            var portString = match.Groups[1].Value;
            var pathString = match.Groups[2].Value;
            if (!int.TryParse(portString, out var port))
            {
                return $"Url {urlTemplate} could not parse port {portString}";
            }

            var url = $"http://{podIp}:{port}/{pathString}";
            using var cts = new CancellationTokenSource(this.config.WatchdogProbeTimeout);
            try
            {
                this.logger.LogDebug($"Probing {url}");
                var resp = await this.httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                resp.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                this.logger.LogDebug($"Error probing {url}: {e}");
                return e.Message;
            }

            return null;
        }
    }
}
