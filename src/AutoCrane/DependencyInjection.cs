// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using AutoCrane.Apps;
using AutoCrane.Interfaces;
using AutoCrane.Models;
using AutoCrane.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace AutoCrane
{
    internal static class DependencyInjection
    {
        internal static void Setup(IConfiguration configuration, IServiceCollection services)
        {
            services.AddSingleton<IFailingPodGetter, KubernetesClient>();
            services.AddSingleton<IPodGetter, KubernetesClient>();
            services.AddSingleton<IPodGetter, KubernetesClient>();
            services.AddSingleton<IExpiredObjectDeleter, KubernetesClient>();
            services.AddSingleton<IEndpointAnnotationAccessor, KubernetesClient>();

            services.AddSingleton<IWatchdogStatusPutter, WatchdogStatusPutter>();
            services.AddSingleton<IWatchdogStatusGetter, WatchdogStatusGetter>();
            services.AddSingleton<IPodAnnotationPutter, PodAnnotationPutter>();
            services.AddSingleton<IPodEvicter, PodEvicter>();
            services.AddSingleton<IKubernetesConfigProvider, KubernetesConfigProvider>();
            services.AddSingleton<IWatchdogStatusAggregator, WatchdogStatusAggregator>();
            services.AddSingleton<IConsecutiveHealthMonitor, ConsecutiveHealthMonitor>();
            services.AddSingleton<IUptimeMonitor, UptimeMonitor>();
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
            services.AddSingleton<ILeaderElection, LeaderElection>();
            services.AddSingleton<IDataRepositoryKnownGoodAccessor, DataRepositoryKnownGoodAccessor>();
            services.AddSingleton<IDataRepositoryLatestVersionAccessor, DataRepositoryLatestVersionAccessor>();
            services.AddSingleton<IDataDeploymentRequestProcessor, DataDeploymentRequestProcessor>();
            services.AddSingleton<IDataRepositoryUpgradeOracleFactory, DataRepositoryUpgradeOracleFactory>();
            services.AddSingleton<ISecretCache, SecretCache>();
            services.AddSingleton<ICredentialHelper, CredentialHelper>();
            services.AddSingleton<ICredentialProvider, CredentialProviderForEnvironmentVariables>();
            services.AddSingleton<ICredentialProvider, CredentialProviderForAzureManagedIdentity>();
            services.AddSingleton<ICredentialProvider, CredentialProviderForAzureKeyVault>();
            services.AddSingleton<ICredentialProvider, CredentialProviderForAzureDevOps>();
            services.AddSingleton<IDurationParser, DurationParser>();

            services.AddSingleton<CredentialProviderForAzureManagedIdentity>();
            services.AddSingleton<KubernetesClient>();
            services.AddSingleton<WatchdogProber>();
            services.AddSingleton<GetWatchdogService>();
            services.AddSingleton<PostWatchdogService>();
            services.AddSingleton<Orchestrator>();
            services.AddSingleton<DataDeployInit>();
            services.AddSingleton<SecretWriter>();
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
                });

                var otlpEndpoint = Environment.GetEnvironmentVariable("OTLP_ENDPOINT");
                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    // Adding the OtlpExporter creates a GrpcChannel.
                    // This switch must be set before creating a GrpcChannel/HttpClient when calling an insecure gRPC service.
                    // See: https://docs.microsoft.com/aspnet/core/grpc/troubleshoot#call-insecure-grpc-services-with-net-core-client
                    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

                    services.AddOpenTelemetryTracing((builder) => builder
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("AutoCrane"))
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddOtlpExporter(otlpOptions =>
                        {
                            otlpOptions.Endpoint = new Uri(otlpEndpoint);
                        }));
                }

                logging.AddConsole();
            });

            services.AddOptions();

            services.Configure<WatchdogStatus>(configuration.GetSection("Watchdog"));
            services.Configure<PodIdentifierOptions>(configuration.GetSection("Pod"));
            services.Configure<KubernetesConfig>(configuration.GetSection("Kubeconfig"));
            services.Configure<AutoCraneOptions>(configuration.GetSection("AutoCrane"));
            services.Configure<WatchdogHealthzOptions>(configuration.GetSection("Watchdogs"));
            services.Configure<DataRepoOptions>(configuration.GetSection("DataRepo"));
            services.Configure<SecretWriterOptions>(configuration.GetSection("SecretWriter"));
        }

        internal static ServiceProvider GetServiceProvider(string[] args)
        {
            var services = new ServiceCollection();
            Setup(new ConfigurationBuilder().AddEnvironmentVariables().AddCommandLine(args).Build(), services);
            return services.BuildServiceProvider();
        }
    }
}
