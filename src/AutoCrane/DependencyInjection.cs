// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using AutoCrane.Apps;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using AutoCrane.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Configuration;

namespace AutoCrane
{
    internal static class DependencyInjection
    {
        internal static void Setup(IConfiguration configuration, IServiceCollection services)
        {
            Log.Logger = new LoggerConfiguration()
                    .WriteTo.Async(a => GetConsoleLogger(a))
                    .CreateLogger();

            services.AddSingleton<IWatchdogStatusPutter, WatchdogStatusPutter>();
            services.AddSingleton<IWatchdogStatusGetter, WatchdogStatusGetter>();
            services.AddSingleton<IFailingPodGetter, FailingPodGetter>();
            services.AddSingleton<IPodGetter, PodGetter>();
            services.AddSingleton<IPodAnnotationPutter, PodAnnotationPutter>();
            services.AddSingleton<IPodEvicter, PodEvicter>();
            services.AddSingleton<IKubernetesConfigProvider, KubernetesConfigProvider>();
            services.AddSingleton<IWatchdogStatusAggregator, WatchdogStatusAggregator>();
            services.AddSingleton<IConsecutiveHealthMonitor, ConsecutiveHealthMonitor>();
            services.AddSingleton<IUptimeMonitor, UptimeMonitor>();
            services.AddSingleton<IPodGetter, PodGetter>();
            services.AddSingleton<IPodIdentifierFactory, PodIdentifierFactory>();
            services.AddSingleton<IAutoCraneConfig, AutoCraneConfig>();
            services.AddSingleton<IClock, DefaultClock>();
            services.AddSingleton<IMonkeyWorkload, MonkeyWorkload>();
            services.AddSingleton<IDataDownloader, DataDownloader>();
            services.AddSingleton<IDataLinker, DataLinker>();
            services.AddSingleton<IDataDownloadRequestFactory, DataDownloadRequestFactory>();
            services.AddSingleton<IDataRepositoryManifestReaderFactory, DataRepositoryManifestReaderFactory>();
            services.AddSingleton<IServiceHeartbeat, ServiceHeartbeat>();
            services.AddSingleton<IDataRepositorySyncer, DataRepositorySyncer>();
            services.AddSingleton<IDataRepositoryManifestWriter, DataRepositoryManifestWriter>();
            services.AddSingleton<IDataRepositoryFetcher, DataRepositoryGitFetcher>();
            services.AddSingleton<IProcessRunner, ProcessRunner>();
            services.AddSingleton<IDataRepositoryManifestFetcher, DataRepositoryManifestFetcher>();
            services.AddSingleton<IFileHasher, FileHasher>();
            services.AddSingleton<IPodDataRequestGetter, PodDataRequestGetter>();
            services.AddSingleton<IDataDeploymentRequestProcessor, DataDeploymentRequestProcessor>();
            services.AddSingleton<ISecretCache, SecretCache>();
            services.AddSingleton<ICredentialHelper, CredentialHelper>();
            services.AddSingleton<ICredentialProvider, CredentialProviderForEnvironmentVariables>();
            services.AddSingleton<ICredentialProvider, CredentialProviderForAzureManagedIdentity>();
            services.AddSingleton<ICredentialProvider, CredentialProviderForAzureKeyVault>();
            services.AddSingleton<ICredentialProvider, CredentialProviderForAzureDevOps>();

            services.AddSingleton<CredentialProviderForAzureManagedIdentity>();
            services.AddSingleton<KubernetesClient>();
            services.AddSingleton<WatchdogProber>();
            services.AddSingleton<GetWatchdogService>();
            services.AddSingleton<PostWatchdogService>();
            services.AddSingleton<Orchestrator>();
            services.AddSingleton<DataDeployInit>();
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddFilter((provider, category, level) =>
                {
                    if (category.StartsWith("Microsoft.AspNet"))
                    {
                        return false;
                    }

                    return true;
                }).AddProvider(new Serilog.Extensions.Logging.SerilogLoggerProvider());
            });
            services.AddOptions();

            services.Configure<WatchdogStatus>(configuration.GetSection("Watchdog"));
            services.Configure<PodIdentifierOptions>(configuration.GetSection("Pod"));
            services.Configure<KubernetesConfig>(configuration.GetSection("Kubeconfig"));
            services.Configure<AutoCraneOptions>(configuration.GetSection("AutoCrane"));
            services.Configure<WatchdogHealthzOptions>(configuration.GetSection("Watchdogs"));
            services.Configure<DataRepoOptions>(configuration.GetSection("DataRepo"));
        }

        internal static ServiceProvider GetServiceProvider(string[] args)
        {
            var services = new ServiceCollection();
            Setup(new ConfigurationBuilder().AddEnvironmentVariables().AddCommandLine(args).Build(), services);
            return services.BuildServiceProvider();
        }

        private static void GetConsoleLogger(LoggerSinkConfiguration a)
        {
            a.Console(outputTemplate: "{Level:u1}:{Timestamp:yyyyMMdd HH:mm:ss.fff}: {Message:lj}{NewLine}");
        }
    }
}
