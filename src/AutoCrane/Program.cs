// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoCrane.Apps;
using AutoCrane.Interfaces;
using AutoCrane.Services;
using Microsoft.Extensions.DependencyInjection;
using Prometheus.DotNetRuntime;

namespace AutoCrane
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            var overrideArgs = Environment.GetEnvironmentVariable("AUTOCRANE_ARGS");
            if (args.Length < 1 && string.IsNullOrEmpty(overrideArgs))
            {
                Console.Error.WriteLine("Usage: AutoCrane <mode>");
                Console.Error.WriteLine("Available modes:");
                Console.Error.WriteLine("  orchestrate: Watches for AutoCraneDeployment CRDs, creates Kubernetes pods for apps.");
                Console.Error.WriteLine("  datadeployinit: Init container process to ensure data directories are set up before app runs.");
                Console.Error.WriteLine("  datadeploy: Sidecar which updates app to use new data folders when requested.");
                Console.Error.WriteLine("  datarepo: Downloads data at origin and makes available to cluster.");
                Console.Error.WriteLine("  getwatchdog: Tool to list watchdog status.");
                Console.Error.WriteLine("  postwatchdog: Tool to update watchdog status.");
                Console.Error.WriteLine("  testworkload: A webservice that can fail its watchdogs on demand.");
                Console.Error.WriteLine("  versionwatcher: Probes origin sources for new app and data versions.");
                Console.Error.WriteLine("  watchdogprober: Discovers probe endpoints through pod annotations, probes and sets watchdogs.");
                Console.Error.WriteLine("  watchdoghealthz: Surfaces pod's watchdog health through a /healthz endpoint.");
                Console.Error.WriteLine("  yaml: Generate yaml for installing into cluster.");
                return 1;
            }

            if (!string.IsNullOrEmpty(overrideArgs))
            {
                // args = overrideArgs.Split(' ').Select(i => i.Replace("%20", " ")).ToArray();
                args = overrideArgs.Split(' ');
            }

            var mode = args[0].ToLowerInvariant();
            var newargs = args.Skip(1).ToArray();
            using var collector = DotNetRuntimeStatsBuilder.Default().StartCollecting();
            try
            {
                using var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (object? sender, ConsoleCancelEventArgs eventArgs) =>
                {
                    Console.Error.WriteLine("Handling Ctrl-C...");
                    cts.Cancel();
                    eventArgs.Cancel = false;
                };

                LeaderElection.LeadershipElectionName = $"acleader{mode}";

                switch (mode)
                {
                    case "orchestrate":
                        return ConsoleWrapper<Orchestrator>(newargs, cts.Token);

                    case "getwatchdog":
                        return ConsoleWrapper<GetWatchdogService>(newargs, cts.Token);

                    case "postwatchdog":
                        return ConsoleWrapper<PostWatchdogService>(newargs, cts.Token);

                    case "watchdogprober":
                        return ConsoleWrapper<WatchdogProber>(newargs, cts.Token);

                    case "testworkload":
                        WebHosting.RunWebService<TestWorkload>(newargs);
                        return 0;

                    case "watchdoghealthz":
                        WebHosting.RunWebService<WatchdogHealthz>(newargs);
                        return 0;

                    case "yaml":
                        return GenerateYaml.Run(newargs);

                    case "datadeployinit":
                        return ConsoleWrapper<DataDeployInit>(newargs, cts.Token);

                    case "datadeploy":
                        WebHosting.RunWebService<DataDeployer>(newargs);
                        return 0;

                    case "datarepo":
                        WebHosting.RunWebService<DataRepository>(newargs);
                        return 0;

                    default:
                        Console.Error.WriteLine($"Mode {mode} not found");
                        return 1;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Unhandled exception in main: {e}");
                return 1;
            }
            finally
            {
                Serilog.Log.CloseAndFlush();
            }
        }

        private static int ConsoleWrapper<T>(string[] newargs, CancellationToken token)
            where T : IAutoCraneService
        {
            try
            {
                return DependencyInjection.GetServiceProvider(newargs).GetRequiredService<T>().RunAsync(token).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Unhandled exception: {e}");
                return 1;
            }
        }
    }
}
